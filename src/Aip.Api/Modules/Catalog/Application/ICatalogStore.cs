using System.Text.Json.Nodes;
namespace Aip.Modules.Catalog;
/// <summary>Transaction-scoped persistence port, implemented by PostgreSQL infrastructure.</summary>
public interface ICatalogStore
{
    Guid OrganizationId { get; }
    Guid ActorId { get; }
    Task<int> Execute(string sql, params (string Name, object? Value)[] parameters);
    Task<object?> Scalar(string sql, params (string Name, object? Value)[] parameters);
    Task<List<JsonObject>> Rows(string sql, params (string Name, object? Value)[] parameters);
}
