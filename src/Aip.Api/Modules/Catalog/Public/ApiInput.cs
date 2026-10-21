using Aip.Modules.Catalog.Domain;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Aip.Modules.Catalog;

public sealed class ApiProblem(int status, string code, string title) : Exception(title)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public static partial class Rules
{
    public static readonly string[] EntityTypes = ["core.business_capability", "core.application", "core.data_asset"];
    public static readonly string[] RelationshipTypes = ["core.supports", "core.uses"];
    public static readonly string[] Classifications = ["internal", "confidential", "restricted"];
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.NonBacktracking)]
    private static partial Regex CodePattern();
    [GeneratedRegex("^\"[1-9][0-9]*\"$", RegexOptions.NonBacktracking)]
    private static partial Regex EtagPattern();
    public static Guid Id(string input) => Guid.TryParse(input, out var id) ? id : throw new ApiProblem(400, "CAT_INVALID_ID", "Identificador inválido");
    public static async Task<JsonObject> Body(HttpRequest request, string[] allowed)
    {
        JsonNode? node;
        try
        {
            using var doc = await JsonDocument.ParseAsync(request.Body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) throw Invalid("Se esperaba un objeto JSON");
            var fields = doc.RootElement.EnumerateObject().Select(x => x.Name).ToArray();
            if (fields.Distinct().Count() != fields.Length) throw new ApiProblem(400, "CAT_INVALID_JSON", "Campos JSON repetidos");
            node = JsonNode.Parse(doc.RootElement.GetRawText());
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { throw new ApiProblem(400, "CAT_INVALID_JSON", "JSON malformado"); }
        var body = node!.AsObject();
        if (body.Any(x => !allowed.Contains(x.Key))) throw Invalid("Campo desconocido o reservado");
        return body;
    }
    public static ApiProblem Invalid(string message) => new(422, "CAT_VALIDATION", message);
    public static string Text(JsonObject body, string key, int max, bool required = true, bool trim = false)
    {
        if (!body.TryGetPropertyValue(key, out var node) || node is null)
        {
            if (required) throw Invalid($"El campo {key} es obligatorio");
            return "";
        }
        string text;
        try
        {
            if (node is not JsonValue value || !value.TryGetValue<string>(out var parsed)) throw Invalid($"El campo {key} debe ser texto");
            text = parsed;
        }
        catch (InvalidOperationException) { throw Invalid($"Texto Unicode inválido para {key}"); }
        text = CatalogDefinitions.Text(text, max, trim);
        if (required && text.Length == 0) throw Invalid($"Longitud inválida para {key}");
        return text;
    }
    public static string? Description(JsonObject body) => !body.ContainsKey("description") || body["description"] is null ? null : Text(body, "description", 4000, false);
    public static string Code(JsonObject body)
    {
        var code = Text(body, "code", 64, true, true);
        if (!CodePattern().IsMatch(code)) throw Invalid("Código inválido");
        return code.ToUpperInvariant();
    }
    public static string Type(JsonObject body, bool relationship)
    {
        var type = Text(body, "type_id", 80);
        if (!(relationship ? RelationshipTypes : EntityTypes).Contains(type)) throw Invalid("Tipo no admitido");
        if (Text(body, "schema_version", 20) != "0.1.0") throw Invalid("Versión de esquema no admitida");
        return type;
    }
    public static string Classification(JsonObject body)
    {
        var c = body.ContainsKey("classification") ? Text(body, "classification", 20) : "internal";
        if (!Classifications.Contains(c)) throw Invalid("Clasificación inválida");
        return c;
    }
    public static int Match(HttpRequest request)
    {
        var v = request.Headers.IfMatch.ToString();
        if (v.Length == 0) throw new ApiProblem(428, "CAT_REVISION_REQUIRED", "Falta If-Match");
        if (!EtagPattern().IsMatch(v) || !int.TryParse(v[1..^1], out var revision))
            throw new ApiProblem(400, "CAT_INVALID_ETAG", "If-Match debe contener una revisión concreta");
        return revision;
    }
}
