using Aip.Modules.Catalog;
namespace Aip.Infrastructure.Postgres;

internal static class PostgresPages
{
    internal static async Task<Page<T>> Read<T>(DbSession db, CatalogQuery query, ResourceKind kind)
    {
        var clauses = new List<string> { "organization_id=@org" };
        var parameters = new List<(string Name, object? Value)> { ("org", db.OrganizationId) };
        void Equal(string column, object? value)
        {
            if (value is null) return;
            clauses.Add(column + "=@" + column); parameters.Add((column, value));
        }
        if (kind == ResourceKind.AuditEvents) { Equal("resource_kind", query.AuditResourceKind); Equal("resource_id", query.ResourceId); }
        else
        {
            if (query.Status != "all") Equal("status", query.Status);
            Equal("type_id", query.TypeId);
            if (kind == ResourceKind.Relationships)
            {
                Equal("source_entity_id", query.SourceId); Equal("target_entity_id", query.TargetId);
                if (query.EntityId is { } entity)
                {
                    clauses.Add("(source_entity_id=@entity_id OR target_entity_id=@entity_id)"); parameters.Add(("entity_id", entity));
                }
            }
            else
            {
                Equal("code", query.Code);
                if (query.Search is { } q) { clauses.Add("strpos(lower(name),lower(@q))>0"); parameters.Add(("q", q)); }
            }
        }
        // Identifiers come exclusively from this adapter; all client values remain parameters.
        var table = kind switch { ResourceKind.Entities => "catalog.entities", ResourceKind.Relationships => "catalog.relationships", _ => "audit.catalog_events" };
        var order = kind switch { ResourceKind.Entities => "code,id", ResourceKind.Relationships => "created_at,id", _ => "occurred_at,id" };
        var where = string.Join(" AND ", clauses);
        var total = (long)(await db.Scalar($"SELECT count(*) FROM {table} WHERE {where}", parameters.ToArray()))!;
        parameters.Add(("limit", query.Limit)); parameters.Add(("offset", query.Offset));
        var items = await db.Rows<T>($"SELECT to_jsonb(t)::text FROM {table} t WHERE {where} ORDER BY {order} LIMIT @limit OFFSET @offset", parameters.ToArray());
        return new(items, total, query.Limit, query.Offset);
    }
}
