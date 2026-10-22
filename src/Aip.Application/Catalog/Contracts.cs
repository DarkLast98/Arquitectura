using Aip.Modules.Catalog.Domain;

namespace Aip.Modules.Catalog;

public enum ResourceKind { Entities, Relationships, AuditEvents }
public abstract record ResourceRecord(Guid Id, Guid OrganizationId, string TypeId, string SchemaVersion,
    string? Description, string Status, int Revision, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, Guid CreatedBy, Guid UpdatedBy);
public sealed record EntityRecord(Guid Id, Guid OrganizationId, string TypeId, string SchemaVersion, string Code,
    string Name, string? Description, string Classification, string Status, int Revision, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, Guid CreatedBy, Guid UpdatedBy)
    : ResourceRecord(Id, OrganizationId, TypeId, SchemaVersion, Description, Status, Revision, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
{
    public ArchitectureEntity Aggregate() => ArchitectureEntity.Restore(Id, OrganizationId, TypeId, SchemaVersion,
        Code, Name, Description, Classification, Status, Revision);
}
public sealed record RelationshipRecord(Guid Id, Guid OrganizationId, string TypeId, string SchemaVersion,
    Guid SourceEntityId, Guid TargetEntityId, string? Description, string Status, int Revision, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, Guid CreatedBy, Guid UpdatedBy)
    : ResourceRecord(Id, OrganizationId, TypeId, SchemaVersion, Description, Status, Revision, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy);
public sealed record Page<T>(IReadOnlyList<T> Items, long Total, int Limit, int Offset);
public sealed record CreateEntity(string TypeId, string SchemaVersion, string Code, string Name,
    string? Description, string Classification, string[] Fields);
public sealed record CreateRelationship(string TypeId, string SchemaVersion, Guid SourceId, Guid TargetId,
    string? Description, string[] Fields);
public readonly record struct Change<T>(bool Supplied, T Value);
public sealed record EditEntity(Change<string> Name, Change<string?> Description, Change<string> Classification)
{
    public string[] Fields => new[] { Name.Supplied ? "name" : null, Description.Supplied ? "description" : null,
        Classification.Supplied ? "classification" : null }.OfType<string>().Order().ToArray();
}

// These ports describe catalog operations, never SQL, HTTP, JSON or database drivers.
// Implementations share the authorized organization transaction and must not commit independently.
public interface ICatalogRepository
{
    Task<EntityRecord?> Entity(Guid id, bool locked = false);
    Task<RelationshipRecord?> Relationship(Guid id, bool locked = false);
    Task Insert(ArchitectureEntity entity);
    Task Insert(ArchitectureRelationship relationship, string? description);
    Task Save(ArchitectureEntity entity, int expectedRevision);
    Task ArchiveRelationship(Guid id, int expectedRevision);
    Task<int> ActiveRelationships(Guid entityId);
    Task<Page<EntityRecord>> Entities(CatalogQuery query);
    Task<Page<RelationshipRecord>> Relationships(CatalogQuery query);
}
