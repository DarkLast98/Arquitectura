using Npgsql;
namespace Aip.Infrastructure.Postgres;

// The composition root owns this singleton; callers never receive the database driver.
public sealed class PostgresConnections(string connectionString) : IDisposable
{
    private readonly NpgsqlDataSource source = NpgsqlDataSource.Create(connectionString);
    internal ValueTask<NpgsqlConnection> Open() => source.OpenConnectionAsync();
    public void Dispose() => source.Dispose();
}
