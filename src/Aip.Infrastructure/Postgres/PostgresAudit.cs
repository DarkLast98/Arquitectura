using Aip.Modules.Audit;
using Aip.Modules.Catalog;
namespace Aip.Infrastructure.Postgres;

public sealed class PostgresAudit(DbSession db) : IAuditPort
{
    public async Task Record(AuditChange change) => await db.Execute(
        "INSERT INTO audit.catalog_events(id,organization_id,actor_id,operation,resource_kind,resource_id,previous_revision,new_revision,correlation_id,changed_fields) VALUES(@id,@org,@actor,@operation,@kind,@resource,@previous,@revision,@correlation,@fields)",
        ("id", Guid.NewGuid()), ("org", db.OrganizationId), ("actor", db.ActorId), ("operation", change.Operation),
        ("kind", change.Relationship ? "relationship" : "entity"), ("resource", change.ResourceId), ("previous", change.PreviousRevision),
        ("revision", change.Revision), ("correlation", change.CorrelationId), ("fields", change.Fields));
    public Task<Page<AuditEvent>> Events(CatalogQuery query) => PostgresPages.Read<AuditEvent>(db, query, ResourceKind.AuditEvents);
}
