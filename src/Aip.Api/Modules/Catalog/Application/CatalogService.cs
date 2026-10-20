using System.Text;
using Aip.Modules.Catalog.Domain;
using Aip.Modules.Audit;
using System.Text.Json.Nodes;

namespace Aip.Modules.Catalog;

public sealed class CatalogService(ICatalogStore db, AuditWriter audit)
{
    public static object[] Types() => [
        new { type_id="core.business_capability",category="entity",schema_version="0.1.0",source_type=(string?)null,target_type=(string?)null },
        new { type_id="core.application",category="entity",schema_version="0.1.0",source_type=(string?)null,target_type=(string?)null },
        new { type_id="core.data_asset",category="entity",schema_version="0.1.0",source_type=(string?)null,target_type=(string?)null },
        new { type_id="core.supports",category="relationship",schema_version="0.1.0",source_type=(string?)"core.application",target_type=(string?)"core.business_capability" },
        new { type_id="core.uses",category="relationship",schema_version="0.1.0",source_type=(string?)"core.application",target_type=(string?)"core.data_asset" }
    ];
    private static string Table(bool relationship) => relationship ? "catalog.relationships" : "catalog.entities";
    public async Task<JsonObject> Get(Guid id, bool relationship, bool locked = false)
    {
        var rows = await db.Rows($"SELECT to_jsonb(t)::text FROM {Table(relationship)} t WHERE organization_id=@org AND id=@id" + (locked ? " FOR UPDATE" : ""), ("org", db.OrganizationId), ("id", id));
        return rows.FirstOrDefault() ?? throw new ApiProblem(404, "CAT_NOT_FOUND", "Recurso no encontrado");
    }
    public async Task<JsonObject> CreateEntity(HttpContext ctx)
    {
        var body = await Rules.Body(ctx.Request, ["type_id", "schema_version", "code", "name", "description", "classification"]);
        var type = Rules.Type(body, false); var code = Rules.Code(body); var name = Rules.Text(body, "name", 200, true, true); var description = Rules.Description(body); var classification = Rules.Classification(body); var id = Guid.NewGuid();
        var aggregate = ArchitectureEntity.Create(id, db.OrganizationId, type, "0.1.0", code, name, description, classification);
        name = aggregate.Name; code = aggregate.Code;
        await db.Execute("INSERT INTO catalog.entities(id,organization_id,type_id,schema_version,code,name,description,classification,created_by,updated_by) VALUES(@id,@org,@type,'0.1.0',@code,@name,@description,@classification,@actor,@actor)",
            ("id", id), ("org", db.OrganizationId), ("type", type), ("code", code), ("name", name), ("description", description), ("classification", classification), ("actor", db.ActorId));
        await audit.Record(ctx, id, false, "created", null, 1, body.Select(x => x.Key).Order().ToArray());
        return await Get(id, false);
    }
    public async Task<JsonObject> CreateRelationship(HttpContext ctx)
    {
        var body = await Rules.Body(ctx.Request, ["type_id", "schema_version", "source_entity_id", "target_entity_id", "description"]);
        var type = Rules.Type(body, true); var source = BodyId(body, "source_entity_id"); var target = BodyId(body, "target_entity_id");
        var description = Rules.Description(body);
        if (source == target) throw Rules.Invalid("Los extremos deben ser diferentes");
        // Every operation touching endpoints takes locks in UUID order.
        JsonObject? sourceRow = null, targetRow = null;
        foreach (var id in new[] { source, target }.Order())
        {
            var row = await Get(id, false, true);
            if (row["status"]!.GetValue<string>() != "active") throw new ApiProblem(409, "CAT_ARCHIVED_ENDPOINT", "El extremo está archivado");
            if (id == source) sourceRow = row; else targetRow = row;
        }
        _ = ArchitectureRelationship.Create(Guid.NewGuid(), db.OrganizationId, type, "0.1.0", FromRow(sourceRow!), FromRow(targetRow!));
        var relation = Guid.NewGuid();
        await db.Execute("INSERT INTO catalog.relationships(id,organization_id,type_id,schema_version,source_entity_id,target_entity_id,description,created_by,updated_by) VALUES(@id,@org,@type,'0.1.0',@source,@target,@description,@actor,@actor)",
            ("id", relation), ("org", db.OrganizationId), ("type", type), ("source", source), ("target", target), ("description", description), ("actor", db.ActorId));
        await audit.Record(ctx, relation, true, "created", null, 1, body.Select(x => x.Key).Order().ToArray());
        return await Get(relation, true);
    }
    private static Guid BodyId(JsonObject body, string key)
    {
        var text = Rules.Text(body, key, 80);
        return Guid.TryParse(text, out var id) ? id : throw Rules.Invalid($"Identificador inválido: {key}");
    }
    public async Task<JsonObject> Update(HttpContext ctx, Guid id, bool relationship, bool archive)
    {
        // Visibility is checked before exposing preconditions or business state.
        var row = await Get(id, relationship, true);
        var expected = Rules.Match(ctx.Request); var previous = row["revision"]!.GetValue<int>();
        if (expected != previous) throw new ApiProblem(412, "CAT_REVISION_CONFLICT", "El registro cambió; recarga y revisa tus cambios");
        if (row["status"]!.GetValue<string>() == "archived") throw new ApiProblem(409, "CAT_ARCHIVED", "El registro está archivado");
        string[] fields;
        if (archive)
        {
            if (ctx.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true) throw Rules.Invalid("Archivo no acepta cuerpo");
            if (!relationship)
            {
                var count = await db.Scalar("SELECT count(*) FROM catalog.relationships WHERE organization_id=@org AND status='active' AND (source_entity_id=@id OR target_entity_id=@id)", ("org", db.OrganizationId), ("id", id));
                _ = FromRow(row).Archive(checked((int)(long)count!), expected);
            }
            fields = ["status"];
            await db.Execute($"UPDATE {Table(relationship)} SET status='archived',revision=revision+1,updated_at=clock_timestamp(),updated_by=@actor WHERE organization_id=@org AND id=@id AND revision=@revision", ("actor", db.ActorId), ("org", db.OrganizationId), ("id", id), ("revision", expected));
        }
        else
        {
            var body = await Rules.Body(ctx.Request, ["name", "description", "classification"]);
            if (body.Count == 0) throw Rules.Invalid("Indica al menos un campo");
            var name = body.ContainsKey("name") ? Rules.Text(body, "name", 200, true, true) : row["name"]!.GetValue<string>();
            var description = body.ContainsKey("description") ? Rules.Description(body) : row["description"]?.GetValue<string>();
            var classification = body.ContainsKey("classification") ? Rules.Classification(body) : row["classification"]!.GetValue<string>();
            var edited = FromRow(row).Edit(name, description, classification, expected); name = edited.Name;
            fields = body.Select(x => x.Key).Order().ToArray();
            await db.Execute("UPDATE catalog.entities SET name=@name,description=@description,classification=@classification,revision=revision+1,updated_at=clock_timestamp(),updated_by=@actor WHERE organization_id=@org AND id=@id AND revision=@revision",
                ("name", name), ("description", description), ("classification", classification), ("actor", db.ActorId), ("org", db.OrganizationId), ("id", id), ("revision", expected));
        }
        await audit.Record(ctx, id, relationship, archive ? "archived" : "updated", previous, previous + 1, fields);
        return await Get(id, relationship);
    }
    public async Task<object> List(HttpRequest request, string kind)
    {
        var relationship = kind == "relationships"; var audit = kind == "audit-events";
        var allowed = audit ? new[] { "limit", "offset", "resource_kind", "resource_id" } : relationship ?
            ["limit", "offset", "type_id", "status", "source_entity_id", "target_entity_id", "entity_id"] :
            ["limit", "offset", "type_id", "status", "code", "q"];
        if (request.Query.Any(x => !allowed.Contains(x.Key) || x.Value.Count != 1)) throw FilterError();
        int Number(string key, int fallback, int min, int max) => !request.Query.ContainsKey(key) ? fallback :
            int.TryParse(request.Query[key], out var n) && n >= min && n <= max ? n : throw FilterError();
        var limit = Number("limit", 20, 1, 100); var offset = Number("offset", 0, 0, 10000);
        var clauses = new List<string> { "organization_id=@org" };
        var parameters = new List<(string Name, object? Value)> { ("org", db.OrganizationId) };
        void Equal(string key, object value) { clauses.Add(key + "=@" + key); parameters.Add((key, value)); }
        if (!audit)
        {
            var status = request.Query.ContainsKey("status") ? request.Query["status"].ToString() : "active";
            if (!new[] { "active", "archived", "all" }.Contains(status)) throw FilterError();
            if (status != "all") Equal("status", status);
            if (request.Query.TryGetValue("type_id", out var type))
            {
                if (!(relationship ? Rules.RelationshipTypes : Rules.EntityTypes).Contains(type.ToString())) throw FilterError();
                Equal("type_id", type.ToString());
            }
        }
        if (relationship)
        {
            if (request.Query.ContainsKey("entity_id") && (request.Query.ContainsKey("source_entity_id") || request.Query.ContainsKey("target_entity_id"))) throw FilterError();
            foreach (var key in new[] { "source_entity_id", "target_entity_id", "entity_id" })
                if (request.Query.TryGetValue(key, out var val))
                {
                    var id = Rules.Id(val.ToString());
                    if (key == "entity_id") { clauses.Add("(source_entity_id=@entity_id OR target_entity_id=@entity_id)"); parameters.Add((key, id)); }
                    else Equal(key, id);
                }
        }
        else if (audit)
        {
            if (request.Query.TryGetValue("resource_kind", out var resourceKind))
            {
                if (!new[] { "entity", "relationship" }.Contains(resourceKind.ToString())) throw FilterError();
                Equal("resource_kind", resourceKind.ToString());
            }
            if (request.Query.TryGetValue("resource_id", out var id)) Equal("resource_id", Rules.Id(id.ToString()));
        }
        else
        {
            if (request.Query.TryGetValue("code", out var code)) Equal("code", code.ToString().Trim().ToUpperInvariant());
            if (request.Query.TryGetValue("q", out var q))
            {
                if (q.ToString().EnumerateRunes().Count() > 128) throw FilterError();
                clauses.Add("strpos(lower(name),lower(@q))>0"); parameters.Add(("q", q.ToString()));
            }
        }
        var table = audit ? "audit.catalog_events" : Table(relationship); var where = string.Join(" AND ", clauses);
        var total = (long)(await db.Scalar($"SELECT count(*) FROM {table} WHERE {where}", parameters.ToArray()))!;
        parameters.Add(("limit", limit)); parameters.Add(("offset", offset));
        var order = audit ? "occurred_at,id" : relationship ? "created_at,id" : "code,id";
        var items = await db.Rows($"SELECT to_jsonb(t)::text FROM {table} t WHERE {where} ORDER BY {order} LIMIT @limit OFFSET @offset", parameters.ToArray());
        return new { items, total, limit, offset };
    }
    private static ArchitectureEntity FromRow(JsonObject row) => ArchitectureEntity.Restore(
        Guid.Parse(row["id"]!.GetValue<string>()), Guid.Parse(row["organization_id"]!.GetValue<string>()),
        row["type_id"]!.GetValue<string>(), row["schema_version"]!.GetValue<string>(), row["code"]!.GetValue<string>(),
        row["name"]!.GetValue<string>(), row["description"]?.GetValue<string>(), row["classification"]!.GetValue<string>(),
        row["status"]!.GetValue<string>(), row["revision"]!.GetValue<int>());
    private static ApiProblem FilterError() => new(400, "CAT_INVALID_FILTER", "Parámetro de consulta inválido");
}
