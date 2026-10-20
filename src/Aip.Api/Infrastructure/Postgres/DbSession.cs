using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;

namespace Aip.Infrastructure.Postgres;

public sealed class DbSession(NpgsqlDataSource source) : IAsyncDisposable, Aip.Modules.Catalog.ICatalogStore
{
    public NpgsqlConnection Connection { get; private set; } = null!;
    public NpgsqlTransaction Transaction { get; private set; } = null!;
    public Guid OrganizationId { get; set; }
    public Guid ActorId { get; private set; }
    public string Role { get; set; } = "";
    public async Task Begin(Guid actor)
    {
        ActorId = actor;
        Connection = await source.OpenConnectionAsync();
        Transaction = await Connection.BeginTransactionAsync();
        await Execute("SET LOCAL TIME ZONE 'UTC'");
        await Execute("SELECT set_config('app.subject_id',@actor,true)", ("actor", actor.ToString()));
    }
    public NpgsqlCommand Command(string sql, params (string Name, object? Value)[] parameters)
    {
        var cmd = new NpgsqlCommand(sql, Connection, Transaction);
        foreach (var p in parameters)
            cmd.Parameters.Add(p.Value is null
                ? new NpgsqlParameter(p.Name, NpgsqlDbType.Unknown) { Value = DBNull.Value }
                : new NpgsqlParameter(p.Name, p.Value));
        return cmd;
    }
    public async Task<int> Execute(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = Command(sql, parameters);
        return await cmd.ExecuteNonQueryAsync();
    }
    public async Task<object?> Scalar(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = Command(sql, parameters);
        return await cmd.ExecuteScalarAsync();
    }
    public async Task<List<JsonObject>> Rows(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = Command(sql, parameters);
        await using var reader = await cmd.ExecuteReaderAsync();
        var rows = new List<JsonObject>();
        while (await reader.ReadAsync()) rows.Add(JsonNode.Parse(reader.GetString(0))!.AsObject());
        return rows;
    }
    public async ValueTask DisposeAsync()
    {
        if (Transaction is not null) await Transaction.DisposeAsync();
        if (Connection is not null) await Connection.DisposeAsync();
    }
}
