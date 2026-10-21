using System.Security.Claims;
using System.Text.Json.Nodes;
using Aip.Modules.Catalog;
using Aip.Modules.Catalog.Domain;
using Aip.Modules.Audit;
using Aip.Modules.IdentityAccess;
using Aip.Infrastructure.Postgres;
using Microsoft.AspNetCore.Antiforgery;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var configPath = Environment.GetEnvironmentVariable("AIP_CONFIG") ?? throw new InvalidOperationException("Configura AIP_CONFIG con el archivo privado de I1");
builder.Configuration.AddJsonFile(Path.GetFullPath(configPath), optional: false);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 65536);
builder.Services.AddSingleton(NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Aip") ?? throw new InvalidOperationException("Falta ConnectionStrings:Aip")));
builder.Services.AddScoped<DbSession>(); builder.Services.AddScoped<ICatalogStore>(s => s.GetRequiredService<DbSession>()); builder.Services.AddScoped<CatalogService>(); builder.Services.AddScoped<AuditWriter>();
builder.AddIdentityAccess();
var app = builder.Build();
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.CacheControl = "no-store";
    ctx.Response.Headers.XContentTypeOptions = "nosniff";
    try { await next(); }
    catch (Exception exception)
    {
        var (status, code, title) = exception switch
        {
            ApiProblem p => (p.Status, p.Code, p.Message),
            CatalogFault p => (p.Kind switch { CatalogFailure.Validation => 422, CatalogFailure.NotFound => 404, CatalogFailure.Revision => 412, _ => 409 }, p.Code, p.Message),
            AntiforgeryValidationException => (400, "AUTH_CSRF", "Token CSRF inválido; recarga la sesión"),
            BadHttpRequestException p => (p.StatusCode, "REQUEST_INVALID", "Petición inválida"),
            PostgresException p when p.SqlState == "23505" => (409, p.ConstraintName == "unique_active_relationship" ? "CAT_DUPLICATE_RELATIONSHIP" : "CAT_DUPLICATE_CODE", "El registro ya existe en esta organización"),
            _ => (500, "INTERNAL_ERROR", "No se pudo completar la operación")
        };
        if (status == 500) app.Logger.LogError("Request failed: {Kind}, correlation {Correlation}", exception.GetType().Name, ctx.TraceIdentifier);
        ctx.Response.Clear(); ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { type = "about:blank", title, status, code, correlation_id = ctx.TraceIdentifier }, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: ctx.RequestAborted);
    }
});
app.UseStatusCodePages(async (Microsoft.AspNetCore.Diagnostics.StatusCodeContext c) =>
{
    var ctx = c.HttpContext; var status = ctx.Response.StatusCode;
    await ctx.Response.WriteAsJsonAsync(new { type = "about:blank", title = status == 401 ? "Identificación requerida" : status == 403 ? "Acceso denegado" : "Petición no disponible", status, code = "HTTP_" + status, correlation_id = ctx.TraceIdentifier }, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: ctx.RequestAborted);
});
app.UseAuthentication(); app.UseAuthorization();
app.MapIdentityAccess();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/contracts/schemas/catalog/0.1.0/catalog.schema.json", () => Results.File(Path.Combine(AppContext.BaseDirectory, "contracts", "schemas", "catalog", "0.1.0", "catalog.schema.json"), "application/schema+json"));
app.MapGet("/contracts/openapi/catalog.v1.json", () => Results.File(Path.Combine(AppContext.BaseDirectory, "contracts", "openapi", "catalog.v1.json"), "application/json"));
app.MapGet("/openapi.json", () => Results.Redirect("/contracts/openapi/catalog.v1.json"));
var catalog = app.MapGroup("/api/v1/organizations/{organizationId}/catalog").RequireAuthorization();
catalog.AddEndpointFilter(async (context, next) =>
{
    var http = context.HttpContext; var db = http.RequestServices.GetRequiredService<DbSession>();
    var organization = Rules.Id(http.Request.RouteValues["organizationId"]!.ToString()!);
    var actor = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    await db.Begin(actor);
    var role = await db.Scalar("SELECT role FROM identity.memberships WHERE organization_id=@org AND subject_id=@actor", ("org", organization), ("actor", actor)) as string;
    if (role is null) throw new ApiProblem(403, "AUTH_MEMBERSHIP", "Sin membresía en la organización");
    if (http.Request.Method != "GET" && role == "reader") throw new ApiProblem(403, "AUTH_ROLE", "El rol solo permite lectura");
    if (http.GetEndpoint()?.Metadata.GetMetadata<OrganizationAdminOnly>() is not null && role != "organization_admin") throw new ApiProblem(403, "AUTH_ROLE", "Solo el administrador organizacional consulta auditoría");
    db.OrganizationId = organization; db.Role = role;
    await db.Execute("SELECT set_config('app.organization_id',@org,true)", ("org", organization.ToString()));
    if (http.Request.Method != "GET") await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http);
    var result = await next(context);
    await db.Transaction.CommitAsync();
    return result;
});
catalog.MapGet("/types", () => Results.Ok(CatalogService.Types()));
foreach (var kind in new[] { "entities", "relationships", "audit-events" })
{
    var current = kind;
    var endpoint = catalog.MapGet("/" + current, async (HttpRequest req, CatalogService service) => Results.Ok(await service.List(req, current)));
    if (current == "audit-events") endpoint.WithMetadata(new OrganizationAdminOnly());
}
foreach (var relationship in new[] { false, true })
{
    var current = relationship; var name = current ? "relationships" : "entities";
    catalog.MapGet("/" + name + "/{id}", async (string id, HttpContext ctx, CatalogService service) => Respond(ctx, await service.Get(Rules.Id(id), current)));
    catalog.MapPost("/" + name, async (HttpContext ctx, CatalogService service) =>
    {
        var row = current ? await service.CreateRelationship(ctx) : await service.CreateEntity(ctx);
        ctx.Response.Headers.Location = ctx.Request.Path + "/" + row["id"]!.GetValue<string>();
        return Respond(ctx, row, 201);
    });
    catalog.MapPost("/" + name + "/{id}/archive", async (string id, HttpContext ctx, CatalogService service) => Respond(ctx, await service.Update(ctx, Rules.Id(id), current, true)));
}
catalog.MapPatch("/entities/{id}", async (string id, HttpContext ctx, CatalogService service) => Respond(ctx, await service.Update(ctx, Rules.Id(id), false, false)));
var webRoot = builder.Configuration["Aip:WebRoot"];
if (webRoot is not null && Directory.Exists(webRoot))
{
    var provider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetFullPath(webRoot));
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
    app.MapFallback((HttpContext ctx) => ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/auth") ? Results.NotFound() : Results.File(Path.Combine(webRoot, "index.html"), "text/html"));
}
app.Run();

static IResult Respond(HttpContext ctx, JsonObject row, int status = 200)
{
    ctx.Response.Headers.ETag = "\"" + row["revision"]!.GetValue<int>() + "\"";
    return Results.Json(row, statusCode: status);
}
