using Aip.Modules.Audit;
using Aip.Modules.Catalog.Domain;
using Aip.Modules.IdentityAccess;

namespace Aip.Modules.Catalog;

public sealed class CatalogService(ICatalogRepository catalog, IAuditPort audit, IOrganizationTransaction scope)
{
    public async Task<ResourceRecord> Get(Guid id, bool relationship, bool locked = false)
    {
        ResourceRecord row = relationship ? await catalog.Relationship(id, locked) ?? throw Missing()
            : await catalog.Entity(id, locked) ?? throw Missing();
        return row.OrganizationId == scope.OrganizationId ? row : throw Missing();
    }
    public static void RequireChange(ResourceRecord row, int expected)
    {
        CatalogDefinitions.RequireRevision(row.Revision, expected);
        CatalogDefinitions.RequireActive(row.Status);
    }
    public async Task<EntityRecord> CreateEntity(CreateEntity command, string correlationId)
    {
        var entity = ArchitectureEntity.Create(Guid.NewGuid(), scope.OrganizationId, command.TypeId, command.SchemaVersion,
            command.Code, command.Name, command.Description, command.Classification);
        await catalog.Insert(entity);
        await audit.Record(new(entity.Id, false, "created", null, 1, correlationId, command.Fields.Order().ToArray()));
        return (EntityRecord)await Get(entity.Id, false);
    }
    public async Task<RelationshipRecord> CreateRelationship(CreateRelationship command, string correlationId)
    {
        if (command.SourceId == command.TargetId) throw CatalogDefinitions.Invalid("Los extremos deben ser diferentes");
        if (command.Description is not null) CatalogDefinitions.Text(command.Description, 4000);
        var endpoints = new Dictionary<Guid, ArchitectureEntity>();
        // Same deterministic lock order as entity archive; transactions own all locks until commit.
        foreach (var id in new[] { command.SourceId, command.TargetId }.Order())
        {
            var endpoint = await catalog.Entity(id, true) ?? throw Missing();
            if (endpoint.Status != "active") throw new CatalogFault(CatalogFailure.Conflict, "CAT_ARCHIVED_ENDPOINT", "El extremo está archivado");
            endpoints[id] = endpoint.Aggregate();
        }
        var relationship = ArchitectureRelationship.Create(Guid.NewGuid(), scope.OrganizationId, command.TypeId,
            command.SchemaVersion, endpoints[command.SourceId], endpoints[command.TargetId]);
        await catalog.Insert(relationship, command.Description);
        await audit.Record(new(relationship.Id, true, "created", null, 1, correlationId, command.Fields.Order().ToArray()));
        return (RelationshipRecord)await Get(relationship.Id, true);
    }
    public async Task<EntityRecord> Edit(Guid id, int expected, EditEntity command, string correlationId)
    {
        var row = (EntityRecord)await Get(id, false, true);
        RequireChange(row, expected);
        if (command.Fields.Length == 0) throw CatalogDefinitions.Invalid("Indica al menos un campo");
        var edited = row.Aggregate().Edit(command.Name.Supplied ? command.Name.Value : row.Name,
            command.Description.Supplied ? command.Description.Value : row.Description,
            command.Classification.Supplied ? command.Classification.Value : row.Classification, expected);
        await catalog.Save(edited, expected);
        await audit.Record(new(id, false, "updated", expected, edited.Revision, correlationId, command.Fields));
        return (EntityRecord)await Get(id, false);
    }
    public async Task<ResourceRecord> Archive(Guid id, bool relationship, int expected, string correlationId)
    {
        var row = await Get(id, relationship, true);
        RequireChange(row, expected);
        if (relationship) await catalog.ArchiveRelationship(id, expected);
        else await catalog.Save(((EntityRecord)row).Aggregate().Archive(await catalog.ActiveRelationships(id), expected), expected);
        await audit.Record(new(id, relationship, "archived", expected, expected + 1, correlationId, ["status"]));
        return await Get(id, relationship);
    }
    public Task<Page<EntityRecord>> Entities(CatalogQuery query) => catalog.Entities(query);
    public Task<Page<RelationshipRecord>> Relationships(CatalogQuery query) => catalog.Relationships(query);
    public Task<Page<AuditEvent>> Events(CatalogQuery query) => audit.Events(query);
    private static CatalogFault Missing() => new(CatalogFailure.NotFound, "CAT_NOT_FOUND", "Recurso no encontrado");
}
