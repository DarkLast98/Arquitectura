using Aip.Modules.Audit;
using Aip.Modules.Catalog;
using Aip.Modules.Catalog.Domain;
using Aip.Modules.IdentityAccess;
using Aip.Infrastructure.Postgres;
using Microsoft.AspNetCore.Antiforgery;
using Aip.Adapters.Http;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var configPath = Environment.GetEnvironmentVariable("AIP_CONFIG") ?? throw new InvalidOperationException("Configura AIP_CONFIG con el archivo privado de I1");
builder.Configuration.AddJsonFile(Path.GetFullPath(configPath), optional: false);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 65536);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.AddSingleton(new PostgresConnections(builder.Configuration.GetConnectionString("Aip") ?? throw new InvalidOperationException("Falta ConnectionStrings:Aip")));
builder.Services.AddScoped<DbSession>();
builder.Services.AddScoped<IOrganizationTransaction>(s => s.GetRequiredService<DbSession>());
builder.Services.AddScoped<ICatalogRepository, PostgresCatalog>();
builder.Services.AddScoped<IAuditPort, PostgresAudit>();
builder.Services.AddScoped<IIdentityPort, PostgresIdentity>();
builder.Services.AddScoped<OrganizationAccess>();
builder.Services.AddScoped<IdentityService>();
builder.Services.AddScoped<CatalogService>();
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
            QueryFault p => (400, p.Code, p.Message),
            AccessFault p => (p.Kind == AccessFailure.Unauthenticated ? 401 : 403, p.Code, p.Message),
            AntiforgeryValidationException => (400, "AUTH_CSRF", "Token CSRF inválido; recarga la sesión"),
            BadHttpRequestException p => (p.StatusCode, "REQUEST_INVALID", "Petición inválida"),
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
app.MapCatalog();
var webRoot = builder.Configuration["Aip:WebRoot"];
if (webRoot is not null && Directory.Exists(webRoot))
{
    var provider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetFullPath(webRoot));
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
    app.MapFallback((HttpContext ctx) => ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/auth") ? Results.NotFound() : Results.File(Path.Combine(webRoot, "index.html"), "text/html"));
}
app.Run();
