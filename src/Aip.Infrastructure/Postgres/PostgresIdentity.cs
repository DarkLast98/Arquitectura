using Aip.Modules.IdentityAccess;
namespace Aip.Infrastructure.Postgres;

public sealed class PostgresIdentity(DbSession db) : IIdentityPort
{
    public async Task<Guid?> ResolveVerifiedSubject(string issuer, string subject)
    {
        await db.Execute("SELECT set_config('app.issuer',@issuer,true),set_config('app.external_subject',@subject,true)", ("issuer", issuer), ("subject", subject));
        await db.Execute("INSERT INTO identity.subjects(id,issuer,external_subject) VALUES(@id,@issuer,@subject) ON CONFLICT(issuer,external_subject) DO NOTHING", ("id", Guid.NewGuid()), ("issuer", issuer), ("subject", subject));
        var id = await db.Scalar("SELECT id FROM identity.subjects WHERE issuer=@issuer AND external_subject=@subject AND status='active'", ("issuer", issuer), ("subject", subject));
        return id is Guid actor ? actor : null;
    }
    public async Task<bool> IsActiveSubject() => await db.Scalar("SELECT id FROM identity.subjects WHERE id=@actor AND status='active'", ("actor", db.ActorId)) is not null;
    public async Task<IReadOnlyList<OrganizationMembership>> Organizations() => await db.Rows<OrganizationMembership>(
        "SELECT json_build_object('id',o.id,'name',o.name,'role',m.role)::text FROM identity.organizations o JOIN identity.memberships m ON m.organization_id=o.id WHERE m.subject_id=@actor ORDER BY o.name", ("actor", db.ActorId));
}
