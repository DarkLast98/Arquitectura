using System.Text;
using Aip.Modules.Catalog.Domain;

namespace Aip.Modules.Catalog;

public sealed class QueryFault(string code = "CAT_INVALID_FILTER") : Exception("Parámetro de consulta inválido")
{
    public string Code { get; } = code;
}
public sealed class CatalogQuery
{
    public int Limit { get; private init; } = 20;
    public int Offset { get; private init; }
    public string Status { get; private init; } = "active";
    public string? TypeId { get; private init; }
    public string? Code { get; private init; }
    public string? Search { get; private init; }
    public Guid? SourceId { get; private init; }
    public Guid? TargetId { get; private init; }
    public Guid? EntityId { get; private init; }
    public string? AuditResourceKind { get; private init; }
    public Guid? ResourceId { get; private init; }
    private CatalogQuery() { }
    public static CatalogQuery Parse(IReadOnlyDictionary<string, string[]> input, ResourceKind kind)
    {
        string[] allowed = kind switch
        {
            ResourceKind.AuditEvents => ["limit", "offset", "resource_kind", "resource_id"],
            ResourceKind.Relationships => ["limit", "offset", "type_id", "status", "source_entity_id", "target_entity_id", "entity_id"],
            _ => ["limit", "offset", "type_id", "status", "code", "q"]
        };
        foreach (var (key, values) in input)
        {
            if (!allowed.Contains(key) || values.Length != 1) throw new QueryFault();
            try { CatalogDefinitions.Text(values[0], 65536); }
            catch (CatalogFault) { throw new QueryFault(); }
        }
        string? Value(string key) => input.TryGetValue(key, out var values) ? values[0] : null;
        int Number(string key, int fallback, int min, int max) => Value(key) is not { } text ? fallback :
            int.TryParse(text, out var n) && n >= min && n <= max ? n : throw new QueryFault();
        Guid? Id(string key) => Value(key) is not { } text ? null : Guid.TryParse(text, out var id) ? id : throw new QueryFault("CAT_INVALID_ID");
        var status = Value("status") ?? "active";
        if (!new[] { "active", "archived", "all" }.Contains(status)) throw new QueryFault();
        var types = kind == ResourceKind.Relationships ? CatalogDefinitions.RelationshipTypes : CatalogDefinitions.EntityTypes;
        if (Value("type_id") is { } type && !types.Contains(type)) throw new QueryFault();
        if (Value("q") is { } q && q.EnumerateRunes().Count() > 128) throw new QueryFault();
        if (Value("resource_kind") is { } resource && resource is not ("entity" or "relationship")) throw new QueryFault();
        if (Value("entity_id") is not null && (Value("source_entity_id") is not null || Value("target_entity_id") is not null)) throw new QueryFault();
        return new CatalogQuery
        {
            Limit = Number("limit", 20, 1, 100), Offset = Number("offset", 0, 0, 10000), Status = status,
            TypeId = Value("type_id"), Code = Value("code")?.Trim().ToUpperInvariant(), Search = Value("q"),
            SourceId = Id("source_entity_id"), TargetId = Id("target_entity_id"), EntityId = Id("entity_id"),
            AuditResourceKind = Value("resource_kind"), ResourceId = Id("resource_id")
        };
    }
}
