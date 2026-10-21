namespace Aip.Modules.IdentityAccess;

public enum AccessFailure { Unauthenticated, Membership, Role }
public sealed class AccessFault(AccessFailure kind, string code, string message) : Exception(message)
{
    public AccessFailure Kind { get; } = kind;
    public string Code { get; } = code;
}
public sealed record OrganizationMembership(Guid Id, string Name, string Role);
public interface IOrganizationTransaction
{
    Guid ActorId { get; }
    Guid OrganizationId { get; }
    Task Begin(Guid actor);
    Task<string?> Membership(Guid organization);
    Task SelectOrganization(Guid organization);
    Task Commit();
}
public interface IIdentityPort
{
    // Called only after the driving adapter has validated the complete OIDC protocol.
    Task<Guid?> ResolveVerifiedSubject(string issuer, string subject);
    Task<bool> IsActiveSubject();
    Task<IReadOnlyList<OrganizationMembership>> Organizations();
}
public sealed class IdentityService(IIdentityPort identities, IOrganizationTransaction transaction)
{
    public async Task<Guid?> ResolveVerifiedSubject(string issuer, string subject)
    {
        await transaction.Begin(Guid.Empty);
        var actor = await identities.ResolveVerifiedSubject(issuer, subject);
        if (actor is not null) await transaction.Commit();
        return actor;
    }
    public async Task<IReadOnlyList<OrganizationMembership>> Organizations(Guid actor)
    {
        await transaction.Begin(actor);
        if (!await identities.IsActiveSubject()) throw new AccessFault(AccessFailure.Unauthenticated, "AUTH_SUBJECT", "Identidad revocada");
        var result = await identities.Organizations();
        await transaction.Commit();
        return result;
    }
}
public sealed class OrganizationAccess(IOrganizationTransaction transaction)
{
    public async Task Authorize(Guid actor, Guid organization, bool write, bool adminOnly)
    {
        await transaction.Begin(actor);
        var role = await transaction.Membership(organization);
        if (role is null) throw new AccessFault(AccessFailure.Membership, "AUTH_MEMBERSHIP", "Sin membresía en la organización");
        if (role is not ("architect" or "organization_admin" or "reader") || (write && role == "reader") || (adminOnly && role != "organization_admin"))
            throw new AccessFault(AccessFailure.Role, "AUTH_ROLE", "El rol no permite la operación");
        await transaction.SelectOrganization(organization);
    }
    public Task Commit() => transaction.Commit();
}
