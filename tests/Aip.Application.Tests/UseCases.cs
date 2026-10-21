using Aip.Modules.Audit;
using Aip.Modules.Catalog;
using Aip.Modules.Catalog.Domain;
using Aip.Modules.IdentityAccess;
using Xunit;

namespace Aip.Application.Tests;

public sealed class UseCases
{
    private static readonly Guid AppId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid CapabilityId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    [Fact]
    public async Task CreateUsesAuthorizedOrganizationAndAuditsNormalizedAggregate()
    {
        var fake = new Ports();
        var result = await fake.Service.CreateEntity(new("core.application", "0.1.0", " app-1 ", " App ", null, "internal", ["name", "code"]), "trace-1");
        Assert.Equal(fake.OrganizationId, result.OrganizationId);
        Assert.Equal("APP-1", result.Code); Assert.Equal("App", result.Name);
        var audit = Assert.Single(fake.Audit);
        Assert.Equal(result.Id, audit.ResourceId); Assert.Equal("trace-1", audit.CorrelationId);
        Assert.Equal(new[] { "code", "name" }, audit.Fields);
        Assert.Equal(0, fake.Commits); // Only the inbound boundary commits the shared transaction.
    }
    [Fact]
    public async Task InvalidCommandNeverReachesPersistence()
    {
        var fake = new Ports();
        await Assert.ThrowsAsync<CatalogFault>(() => fake.Service.CreateEntity(new("future.service", "0.2.0", "X", "X", null, "internal", []), "trace"));
        Assert.Empty(fake.EntitiesById); Assert.Empty(fake.Audit);
    }
    [Fact]
    public async Task PartialEditDistinguishesAbsentDescriptionFromExplicitNull()
    {
        var fake = new Ports(); var original = fake.Add(AppId, "core.application") with { Description = "Original" };
        fake.EntitiesById[AppId] = original;
        var edited = await fake.Service.Edit(AppId, 1, new(new(false, ""), new(true, null), new(false, "")), "trace");
        Assert.Equal(original.Name, edited.Name); Assert.Null(edited.Description);
        Assert.Equal(2, edited.Revision); Assert.Equal(original.Code, edited.Code);
        Assert.Equal(new[] { "description" }, Assert.Single(fake.Audit).Fields);
    }
    [Fact]
    public async Task StaleRevisionCannotWriteOrAudit()
    {
        var fake = new Ports(); fake.Add(AppId, "core.application");
        var failure = await Assert.ThrowsAsync<CatalogFault>(() => fake.Service.Edit(AppId, 2, new(new(true, "Changed"), new(false, null), new(false, "")), "trace"));
        Assert.Equal(CatalogFailure.Revision, failure.Kind); Assert.Empty(fake.Audit);
        Assert.Equal(1, fake.EntitiesById[AppId].Revision);
    }
    [Fact]
    public async Task ActiveRelationshipsPreventArchive()
    {
        var fake = new Ports { ActiveEdges = 1 }; fake.Add(AppId, "core.application");
        var failure = await Assert.ThrowsAsync<CatalogFault>(() => fake.Service.Archive(AppId, false, 1, "trace"));
        Assert.Equal("CAT_ACTIVE_RELATIONSHIPS", failure.Code); Assert.Empty(fake.Audit);
        Assert.Equal("active", fake.EntitiesById[AppId].Status);
    }
    [Fact]
    public async Task RelationshipLocksEndpointsInDeterministicOrderBeforeInsert()
    {
        var fake = new Ports(); fake.Add(AppId, "core.application"); fake.Add(CapabilityId, "core.business_capability");
        var relationship = await fake.Service.CreateRelationship(new("core.supports", "0.1.0", AppId, CapabilityId, null, ["type_id"]), "trace");
        Assert.Equal(new[] { CapabilityId, AppId }, fake.Locks);
        Assert.Equal(AppId, relationship.SourceEntityId); Assert.Equal(CapabilityId, relationship.TargetEntityId);
        Assert.True(Assert.Single(fake.Audit).Relationship);
    }
    [Fact]
    public async Task ForeignOrganizationEndpointIsRejectedEvenIfAdapterReturnsIt()
    {
        var fake = new Ports(); fake.Add(AppId, "core.application");
        fake.Add(CapabilityId, "core.business_capability");
        fake.EntitiesById[CapabilityId] = fake.EntitiesById[CapabilityId] with { OrganizationId = Guid.NewGuid() };
        var fault = await Assert.ThrowsAsync<CatalogFault>(() => fake.Service.CreateRelationship(new("core.supports", "0.1.0", AppId, CapabilityId, null, []), "trace"));
        Assert.Equal(CatalogFailure.NotFound, fault.Kind); Assert.Empty(fake.RelationshipsById); Assert.Empty(fake.Audit);
    }
    [Fact]
    public async Task ForeignResourceCannotBeReturnedOrEditedByApplication()
    {
        var fake = new Ports(); fake.Add(AppId, "core.application");
        fake.EntitiesById[AppId] = fake.EntitiesById[AppId] with { OrganizationId = Guid.NewGuid() };
        var fault = await Assert.ThrowsAsync<CatalogFault>(() => fake.Service.Get(AppId, false));
        Assert.Equal(CatalogFailure.NotFound, fault.Kind);
        await Assert.ThrowsAsync<CatalogFault>(() => fake.Service.Edit(AppId, 1, new(new(true, "Changed"), new(false, null), new(false, "")), "trace"));
        Assert.Equal(1, fake.EntitiesById[AppId].Revision); Assert.Empty(fake.Audit);
    }
    [Fact]
    public async Task MissingResourceDoesNotLeakRevisionOrState()
    {
        var fake = new Ports();
        var fault = await Assert.ThrowsAsync<CatalogFault>(() => fake.Service.Archive(AppId, false, 99, "trace"));
        Assert.Equal(CatalogFailure.NotFound, fault.Kind);
    }
    [Fact]
    public async Task AuditFailurePropagatesWithoutCommitting()
    {
        var fake = new Ports { FailAudit = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fake.Service.CreateEntity(new("core.application", "0.1.0", "X", "X", null, "internal", []), "trace"));
        Assert.Equal(0, fake.Commits); // Actual rollback is verified separately against PostgreSQL.
    }
    [Theory]
    [InlineData("reader", false, false, true)]
    [InlineData("reader", true, false, false)]
    [InlineData("architect", true, false, true)]
    [InlineData("architect", false, true, false)]
    [InlineData("organization_admin", true, true, true)]
    [InlineData("unrecognized", true, false, false)]
    [InlineData("unrecognized", false, false, false)]
    [InlineData(null, false, false, false)]
    public async Task OrganizationPolicyAuthorizesBeforeSelectingContext(string? role, bool write, bool adminOnly, bool allowed)
    {
        var fake = new Ports { Role = role }; var org = Guid.NewGuid(); var access = new OrganizationAccess(fake);
        if (allowed) { await access.Authorize(fake.ActorId, org, write, adminOnly); Assert.Equal(org, fake.Selected); }
        else { await Assert.ThrowsAsync<AccessFault>(() => access.Authorize(fake.ActorId, org, write, adminOnly)); Assert.Null(fake.Selected); }
        Assert.Equal(0, fake.Commits);
    }
    [Fact]
    public async Task RevokedSubjectCannotEnumerateOrganizationsOrCommit()
    {
        var fake = new Ports { ActiveSubject = false };
        var fault = await Assert.ThrowsAsync<AccessFault>(() => new IdentityService(fake, fake).Organizations(fake.ActorId));
        Assert.Equal(AccessFailure.Unauthenticated, fault.Kind); Assert.Equal(0, fake.Commits);
    }
    [Fact]
    public void QueryPortRetainsLiteralSearchAndRejectsAmbiguousFilters()
    {
        var query = CatalogQuery.Parse(new Dictionary<string, string[]> { ["q"] = ["%' OR 1=1 --"], ["code"] = [" app "], ["limit"] = ["100"] }, ResourceKind.Entities);
        Assert.Equal("%' OR 1=1 --", query.Search); Assert.Equal("APP", query.Code); Assert.Equal(100, query.Limit);
        Assert.Throws<QueryFault>(() => CatalogQuery.Parse(new Dictionary<string, string[]> { ["q"] = ["one", "two"] }, ResourceKind.Entities));
        Assert.Throws<QueryFault>(() => CatalogQuery.Parse(new Dictionary<string, string[]> { ["entity_id"] = [AppId.ToString()], ["source_entity_id"] = [AppId.ToString()] }, ResourceKind.Relationships));
        Assert.Throws<QueryFault>(() => CatalogQuery.Parse(new Dictionary<string, string[]> { ["offset"] = ["10001"] }, ResourceKind.Entities));
    }
    [Fact]
    public void InnerAssembliesCannotDependOnFrameworksOrAdapters()
    {
        var domain = typeof(ArchitectureEntity).Assembly;
        var application = typeof(CatalogService).Assembly;
        Assert.All(domain.GetReferencedAssemblies(), reference => Assert.True(reference.Name!.StartsWith("System."), reference.Name));
        Assert.All(application.GetReferencedAssemblies(), reference => Assert.True(reference.Name!.StartsWith("System.") || reference.Name == domain.GetName().Name, reference.Name));
    }
}

internal sealed class Ports : ICatalogRepository, IAuditPort, IOrganizationTransaction, IIdentityPort
{
    public Guid ActorId { get; } = Guid.NewGuid();
    public Guid OrganizationId { get; } = Guid.NewGuid();
    public Dictionary<Guid, EntityRecord> EntitiesById { get; } = [];
    public Dictionary<Guid, RelationshipRecord> RelationshipsById { get; } = [];
    public List<AuditChange> Audit { get; } = [];
    public List<Guid> Locks { get; } = [];
    public int ActiveEdges { get; init; }
    public bool FailAudit { get; init; }
    public int Commits { get; private set; }
    public string? Role { get; init; } = "architect";
    public Guid? Selected { get; private set; }
    public bool ActiveSubject { get; init; } = true;
    public CatalogService Service => new(this, this, this);
    public EntityRecord Add(Guid id, string type)
    {
        var now = DateTimeOffset.UtcNow;
        var row = new EntityRecord(id, OrganizationId, type, "0.1.0", id.ToString(), "Original", null, "internal", "active", 1, now, now, ActorId, ActorId);
        EntitiesById[id] = row; return row;
    }
    public Task<EntityRecord?> Entity(Guid id, bool locked = false)
    { if (locked) Locks.Add(id); return Task.FromResult(EntitiesById.GetValueOrDefault(id)); }
    public Task<RelationshipRecord?> Relationship(Guid id, bool locked = false) => Task.FromResult(RelationshipsById.GetValueOrDefault(id));
    public Task Insert(ArchitectureEntity entity)
    { EntitiesById[entity.Id] = Add(entity.Id, entity.TypeId) with { Code = entity.Code, Name = entity.Name, Description = entity.Description, Classification = entity.Classification }; return Task.CompletedTask; }
    public Task Insert(ArchitectureRelationship relationship, string? description)
    { var now = DateTimeOffset.UtcNow; RelationshipsById[relationship.Id] = new(relationship.Id, OrganizationId, relationship.TypeId, relationship.SchemaVersion, relationship.SourceId, relationship.TargetId, description, "active", 1, now, now, ActorId, ActorId); return Task.CompletedTask; }
    public Task Save(ArchitectureEntity entity, int expectedRevision)
    { EntitiesById[entity.Id] = EntitiesById[entity.Id] with { Name = entity.Name, Description = entity.Description, Classification = entity.Classification, Status = entity.Status, Revision = entity.Revision }; return Task.CompletedTask; }
    public Task ArchiveRelationship(Guid id, int expectedRevision) => throw new NotSupportedException();
    public Task<int> ActiveRelationships(Guid entityId) => Task.FromResult(ActiveEdges);
    public Task<Page<EntityRecord>> Entities(CatalogQuery query) => throw new NotSupportedException();
    public Task<Page<RelationshipRecord>> Relationships(CatalogQuery query) => throw new NotSupportedException();
    public Task<Page<AuditEvent>> Events(CatalogQuery query) => throw new NotSupportedException();
    public Task Record(AuditChange change)
    { if (FailAudit) throw new InvalidOperationException("Synthetic audit failure"); Audit.Add(change); return Task.CompletedTask; }
    public Task Begin(Guid actor) => Task.CompletedTask;
    public Task<string?> Membership(Guid organization) => Task.FromResult(Role);
    public Task SelectOrganization(Guid organization) { Selected = organization; return Task.CompletedTask; }
    public Task Commit() { Commits++; return Task.CompletedTask; }
    public Task<Guid?> ResolveVerifiedSubject(string issuer, string subject) => throw new NotSupportedException();
    public Task<bool> IsActiveSubject() => Task.FromResult(ActiveSubject);
    public Task<IReadOnlyList<OrganizationMembership>> Organizations() => throw new NotSupportedException();
}
