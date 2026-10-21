using System.Security.Claims;
using Aip.Modules.Catalog;
using Aip.Modules.IdentityAccess;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;

namespace Aip.Adapters.Http;

public static class CatalogEndpoints
{
    public static void MapCatalog(this WebApplication app)
    {
        var catalog = app.MapGroup("/api/v1/organizations/{organizationId}/catalog").RequireAuthorization();
        catalog.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var access = http.RequestServices.GetRequiredService<OrganizationAccess>();
            await access.Authorize(Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                Rules.Id(http.Request.RouteValues["organizationId"]!.ToString()!), http.Request.Method != "GET",
                http.GetEndpoint()?.Metadata.GetMetadata<OrganizationAdminOnly>() is not null);
            if (http.Request.Method != "GET") await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http);
            var result = await next(context);
            await access.Commit();
            return result;
        });
        catalog.MapGet("/types", () => Results.Ok(Types()));
        catalog.MapGet("/entities", async (HttpRequest request, CatalogService service) => Results.Ok(await service.Entities(Query(request, ResourceKind.Entities))));
        catalog.MapGet("/relationships", async (HttpRequest request, CatalogService service) => Results.Ok(await service.Relationships(Query(request, ResourceKind.Relationships))));
        catalog.MapGet("/audit-events", async (HttpRequest request, CatalogService service) => Results.Ok(await service.Events(Query(request, ResourceKind.AuditEvents))))
            .WithMetadata(new OrganizationAdminOnly());
        catalog.MapPost("/entities", async (HttpContext ctx, CatalogService service) =>
        {
            var body = await Rules.Body(ctx.Request, ["type_id", "schema_version", "code", "name", "description", "classification"]);
            var command = new CreateEntity(Rules.Type(body, false), Rules.Text(body, "schema_version", 20), Rules.Code(body),
                Rules.Text(body, "name", 200, true, true), Rules.Description(body), Rules.Classification(body), body.Select(x => x.Key).ToArray());
            var row = await service.CreateEntity(command, ctx.TraceIdentifier);
            ctx.Response.Headers.Location = ctx.Request.Path + "/" + row.Id;
            return Respond(ctx, row, 201);
        });
        catalog.MapPost("/relationships", async (HttpContext ctx, CatalogService service) =>
        {
            var body = await Rules.Body(ctx.Request, ["type_id", "schema_version", "source_entity_id", "target_entity_id", "description"]);
            Guid Id(string key) => Guid.TryParse(Rules.Text(body, key, 80), out var id) ? id : throw Rules.Invalid($"Identificador inválido: {key}");
            var command = new CreateRelationship(Rules.Type(body, true), Rules.Text(body, "schema_version", 20), Id("source_entity_id"),
                Id("target_entity_id"), Rules.Description(body), body.Select(x => x.Key).ToArray());
            var row = await service.CreateRelationship(command, ctx.TraceIdentifier);
            ctx.Response.Headers.Location = ctx.Request.Path + "/" + row.Id;
            return Respond(ctx, row, 201);
        });
        foreach (var relationship in new[] { false, true })
        {
            var current = relationship; var name = current ? "relationships" : "entities";
            catalog.MapGet("/" + name + "/{id}", async (string id, HttpContext ctx, CatalogService service) => Respond(ctx, await service.Get(Rules.Id(id), current)));
            catalog.MapPost("/" + name + "/{id}/archive", async (string id, HttpContext ctx, CatalogService service) =>
            {
                var resourceId = Rules.Id(id);
                var expected = await Revision(ctx.Request, service, resourceId, current);
                if (ctx.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true) throw Rules.Invalid("Archivo no acepta cuerpo");
                return Respond(ctx, await service.Archive(resourceId, current, expected, ctx.TraceIdentifier));
            });
        }
        catalog.MapPatch("/entities/{id}", async (string id, HttpContext ctx, CatalogService service) =>
        {
            var resourceId = Rules.Id(id);
            var expected = await Revision(ctx.Request, service, resourceId, false);
            var body = await Rules.Body(ctx.Request, ["name", "description", "classification"]);
            var command = new EditEntity(new(body.ContainsKey("name"), body.ContainsKey("name") ? Rules.Text(body, "name", 200, true, true) : ""),
                new(body.ContainsKey("description"), Rules.Description(body)),
                new(body.ContainsKey("classification"), Rules.Classification(body)));
            return Respond(ctx, await service.Edit(resourceId, expected, command, ctx.TraceIdentifier));
        });
    }
    private static CatalogQuery Query(HttpRequest request, ResourceKind kind) => CatalogQuery.Parse(
        request.Query.ToDictionary(x => x.Key, x => x.Value.Select(v => v ?? "").ToArray()), kind);
    private static async Task<int> Revision(HttpRequest request, CatalogService service, Guid id, bool relationship)
    {
        // Visibility precedes transport preconditions; malformed bodies cannot disclose hidden resources.
        var row = await service.Get(id, relationship, true);
        var expected = Rules.Match(request);
        CatalogService.RequireChange(row, expected);
        return expected;
    }
    private static IResult Respond(HttpContext ctx, ResourceRecord row, int status = 200)
    {
        ctx.Response.Headers.ETag = "\"" + row.Revision + "\"";
        return Results.Json((object)row, statusCode: status);
    }
    private static object[] Types() => [
        new { type_id="core.business_capability",category="entity",schema_version="0.1.0",source_type=(string?)null,target_type=(string?)null },
        new { type_id="core.application",category="entity",schema_version="0.1.0",source_type=(string?)null,target_type=(string?)null },
        new { type_id="core.data_asset",category="entity",schema_version="0.1.0",source_type=(string?)null,target_type=(string?)null },
        new { type_id="core.supports",category="relationship",schema_version="0.1.0",source_type=(string?)"core.application",target_type=(string?)"core.business_capability" },
        new { type_id="core.uses",category="relationship",schema_version="0.1.0",source_type=(string?)"core.application",target_type=(string?)"core.data_asset" }
    ];
}
