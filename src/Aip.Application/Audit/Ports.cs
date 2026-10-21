using Aip.Modules.Catalog;
namespace Aip.Modules.Audit;

public sealed record AuditEvent(Guid Id, Guid OrganizationId, Guid ActorId, string Operation, string ResourceKind,
    Guid ResourceId, int? PreviousRevision, int NewRevision, DateTimeOffset OccurredAt, string CorrelationId, string[] ChangedFields);

public sealed record AuditChange(Guid ResourceId, bool Relationship, string Operation, int? PreviousRevision,
    int Revision, string CorrelationId, string[] Fields);

public interface IAuditPort
{
    Task Record(AuditChange change);
    Task<Page<AuditEvent>> Events(CatalogQuery query);
}
