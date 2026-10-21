using System.Text.Json;
using Aip.Modules.Catalog.Domain;
using Aip.Modules.IdentityAccess;
using Npgsql;
using NpgsqlTypes;

namespace Aip.Infrastructure.Postgres;

public sealed class DbSession(PostgresConnections source) : IAsyncDisposable, IOrganizationTransaction
{
    internal NpgsqlConnection Connection { get; private set; } = null!;
    internal NpgsqlTransaction Transaction { get; private set; } = null!;
    public Guid OrganizationId { get; private set; }
    public Guid ActorId { get; private set; }
    public async Task Begin(Guid actor)
    {
        if (Connection is not null) throw new InvalidOperationException("La transacción organizacional ya está abierta");
        ActorId = actor;
        Connection = await source.Open();
        Transaction = await Connection.BeginTransactionAsync();
        await Execute("SET LOCAL TIME ZONE 'UTC'");
        await Execute("SELECT set_config('app.subject_id',@actor,true)", ("actor", actor.ToString()));
    }
    internal NpgsqlCommand Command(string sql, params (string Name, object? Value)[] parameters)
    {
        var cmd = new NpgsqlCommand(sql, Connection, Transaction);
        foreach (var p in parameters)
            cmd.Parameters.Add(p.Value is null
                ? new NpgsqlParameter(p.Name, NpgsqlDbType.Unknown) { Value = DBNull.Value }
                : new NpgsqlParameter(p.Name, p.Value));
        return cmd;
    }
    internal async Task<int> Execute(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = Command(sql, parameters);
        try { return await cmd.ExecuteNonQueryAsync(); }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new CatalogFault(CatalogFailure.Conflict, ex.ConstraintName == "unique_active_relationship"
                ? "CAT_DUPLICATE_RELATIONSHIP" : "CAT_DUPLICATE_CODE", "El registro ya existe en esta organización");
        }
    }
    internal async Task<object?> Scalar(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = Command(sql, parameters);
        return await cmd.ExecuteScalarAsync();
    }
    internal async Task<List<T>> Rows<T>(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = Command(sql, parameters);
        await using var reader = await cmd.ExecuteReaderAsync();
        var rows = new List<T>();
        while (await reader.ReadAsync()) rows.Add(JsonSerializer.Deserialize<T>(reader.GetString(0), JsonOptions)!);
        return rows;
    }
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    public async Task<string?> Membership(Guid organization) => await Scalar(
        "SELECT role FROM identity.memberships WHERE organization_id=@org AND subject_id=@actor", ("org", organization), ("actor", ActorId)) as string;
    public async Task SelectOrganization(Guid organization)
    {
        await Execute("SELECT set_config('app.organization_id',@org,true)", ("org", organization.ToString()));
        OrganizationId = organization;
    }
    public Task Commit() => Transaction.CommitAsync();
    public async ValueTask DisposeAsync()
    {
        if (Transaction is not null) await Transaction.DisposeAsync();
        if (Connection is not null) await Connection.DisposeAsync();
    }
}
