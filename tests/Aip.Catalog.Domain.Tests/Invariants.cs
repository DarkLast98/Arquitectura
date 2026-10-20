using Aip.Modules.Catalog.Domain;
using Xunit;

namespace Aip.Catalog.Domain.Tests;

public class Invariants
{
    private static readonly Guid Org = Guid.NewGuid();
    private static ArchitectureEntity Entity(string type = "core.application", Guid? org = null, string name = " Aplicación ") => ArchitectureEntity.Create(Guid.NewGuid(), org ?? Org, type, "0.1.0", " app-01 ", name, null, "internal");
    [Fact]
    public void IdentityAndCodeSurviveEdits()
    {
        var original = Entity(); var edited = original.Edit("Cambio", null, "restricted", 1);
        Assert.Equal("APP-01", original.Code); Assert.Equal("Aplicación", original.Name);
        Assert.Equal(original.Id, edited.Id); Assert.Equal(original.OrganizationId, edited.OrganizationId);
        Assert.Equal(original.Code, edited.Code); Assert.Equal(2, edited.Revision); Assert.Equal(1, original.Revision);
    }
    [Theory]
    [InlineData("core.supports", "core.business_capability")]
    [InlineData("core.uses", "core.data_asset")]
    public void RelationsRespectDirectionAndTenant(string type, string targetType)
    {
        var app = Entity(); var target = Entity(targetType);
        Assert.Equal(target.Id, ArchitectureRelationship.Create(Guid.NewGuid(), Org, type, "0.1.0", app, target).TargetId);
        Assert.Equal(CatalogFailure.Validation, Assert.Throws<CatalogFault>(() => ArchitectureRelationship.Create(Guid.NewGuid(), Org, type, "0.1.0", target, app)).Kind);
        Assert.Equal(CatalogFailure.NotFound, Assert.Throws<CatalogFault>(() => ArchitectureRelationship.Create(Guid.NewGuid(), Org, type, "0.1.0", app, Entity(targetType, Guid.NewGuid()))).Kind);
    }
    [Fact]
    public void ArchiveAndRevisionGuardHistory()
    {
        var entity = Entity(); Assert.Equal(CatalogFailure.Conflict, Assert.Throws<CatalogFault>(() => entity.Archive(1, 1)).Kind);
        Assert.Equal(CatalogFailure.Revision, Assert.Throws<CatalogFault>(() => entity.Edit("Cambio", null, "internal", 2)).Kind);
        var archived = entity.Archive(0, 1); Assert.Equal("archived", archived.Status);
        Assert.Equal(CatalogFailure.Conflict, Assert.Throws<CatalogFault>(() => archived.Edit("Cambio", null, "internal", 2)).Kind);
        Assert.Throws<CatalogFault>(() => ArchitectureRelationship.Create(Guid.NewGuid(), Org, "core.supports", "0.1.0", archived, Entity("core.business_capability")));
    }
    [Fact]
    public void UnicodeLimitsCountCharactersAndRejectFutureMetamodel()
    {
        Assert.Equal(200, Entity(name: string.Concat(Enumerable.Repeat("😀", 200))).Name.EnumerateRunes().Count());
        Assert.Equal(200, Entity(name: "  " + new string('x', 200) + "  ").Name.Length);
        Assert.Throws<CatalogFault>(() => Entity(name: new string('x', 201)));
        Assert.Throws<CatalogFault>(() => Entity("future.service"));
        Assert.Throws<CatalogFault>(() => ArchitectureEntity.Create(Guid.NewGuid(), Org, "core.application", "0.2.0", "CODE", "Name", null, "internal"));
    }
}
