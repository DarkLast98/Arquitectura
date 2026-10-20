using Aip.Infrastructure.Postgres;

namespace Aip.Modules.Audit;

/// <summary>Owns audit writes while sharing the caller's transaction.</summary>
public sealed class AuditWriter(DbSession db)
{
    public Task<int> Record(HttpContext context, Guid resourceId, bool relationship, string operation, int? previousRevision, int revision, string[] fields) =>
        db.Execute("INSERT INTO audit.catalog_events(id,organization_id,actor_id,operation,resource_kind,resource_id,previous_revision,new_revision,correlation_id,changed_fields) VALUES(@id,@org,@actor,@operation,@kind,@resource,@previous,@revision,@correlation,@fields)",
            ("id", Guid.NewGuid()), ("org", db.OrganizationId), ("actor", db.ActorId), ("operation", operation),
            ("kind", relationship ? "relationship" : "entity"), ("resource", resourceId), ("previous", previousRevision),
            ("revision", revision), ("correlation", context.TraceIdentifier), ("fields", fields));
}
