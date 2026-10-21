using Aip.Modules.Catalog;
using Aip.Modules.Catalog.Domain;
namespace Aip.Infrastructure.Postgres;

public sealed class PostgresCatalog(DbSession db) : ICatalogRepository
{
    public async Task<EntityRecord?> Entity(Guid id, bool locked = false) => (await db.Rows<EntityRecord>(
        "SELECT to_jsonb(t)::text FROM catalog.entities t WHERE organization_id=@org AND id=@id" + (locked ? " FOR UPDATE" : ""),
        ("org", db.OrganizationId), ("id", id))).FirstOrDefault();
    public async Task<RelationshipRecord?> Relationship(Guid id, bool locked = false) => (await db.Rows<RelationshipRecord>(
        "SELECT to_jsonb(t)::text FROM catalog.relationships t WHERE organization_id=@org AND id=@id" + (locked ? " FOR UPDATE" : ""),
        ("org", db.OrganizationId), ("id", id))).FirstOrDefault();
    public async Task Insert(ArchitectureEntity entity) => await db.Execute(
        "INSERT INTO catalog.entities(id,organization_id,type_id,schema_version,code,name,description,classification,created_by,updated_by) VALUES(@id,@org,@type,@schema,@code,@name,@description,@classification,@actor,@actor)",
        ("id", entity.Id), ("org", db.OrganizationId), ("type", entity.TypeId), ("schema", entity.SchemaVersion), ("code", entity.Code),
        ("name", entity.Name), ("description", entity.Description), ("classification", entity.Classification), ("actor", db.ActorId));
    public async Task Insert(ArchitectureRelationship relationship, string? description) => await db.Execute(
        "INSERT INTO catalog.relationships(id,organization_id,type_id,schema_version,source_entity_id,target_entity_id,description,created_by,updated_by) VALUES(@id,@org,@type,@schema,@source,@target,@description,@actor,@actor)",
        ("id", relationship.Id), ("org", db.OrganizationId), ("type", relationship.TypeId), ("schema", relationship.SchemaVersion),
        ("source", relationship.SourceId), ("target", relationship.TargetId), ("description", description), ("actor", db.ActorId));
    public async Task Save(ArchitectureEntity entity, int expectedRevision)
    {
        var changed = await db.Execute("UPDATE catalog.entities SET name=@name,description=@description,classification=@classification,status=@status,revision=revision+1,updated_at=clock_timestamp(),updated_by=@actor WHERE organization_id=@org AND id=@id AND revision=@revision",
            ("name", entity.Name), ("description", entity.Description), ("classification", entity.Classification), ("status", entity.Status),
            ("actor", db.ActorId), ("org", db.OrganizationId), ("id", entity.Id), ("revision", expectedRevision));
        CatalogDefinitions.RequireRevision(changed, 1);
    }
    public async Task ArchiveRelationship(Guid id, int expectedRevision)
    {
        var changed = await db.Execute("UPDATE catalog.relationships SET status='archived',revision=revision+1,updated_at=clock_timestamp(),updated_by=@actor WHERE organization_id=@org AND id=@id AND revision=@revision",
            ("actor", db.ActorId), ("org", db.OrganizationId), ("id", id), ("revision", expectedRevision));
        CatalogDefinitions.RequireRevision(changed, 1);
    }
    public async Task<int> ActiveRelationships(Guid entityId) => checked((int)(long)(await db.Scalar(
        "SELECT count(*) FROM catalog.relationships WHERE organization_id=@org AND status='active' AND (source_entity_id=@id OR target_entity_id=@id)",
        ("org", db.OrganizationId), ("id", entityId)))!);
    public Task<Page<EntityRecord>> Entities(CatalogQuery query) => PostgresPages.Read<EntityRecord>(db, query, ResourceKind.Entities);
    public Task<Page<RelationshipRecord>> Relationships(CatalogQuery query) => PostgresPages.Read<RelationshipRecord>(db, query, ResourceKind.Relationships);
}
