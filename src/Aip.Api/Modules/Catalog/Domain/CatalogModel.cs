using System.Text;
using System.Buffers;
using System.Text.RegularExpressions;

namespace Aip.Modules.Catalog.Domain;

public enum CatalogFailure { Validation, Conflict, NotFound, Revision }
public sealed class CatalogFault(CatalogFailure kind, string code, string message) : Exception(message)
{
    public CatalogFailure Kind { get; } = kind;
    public string Code { get; } = code;
}
public static class CatalogDefinitions
{
    public const string SchemaVersion = "0.1.0";
    public static IReadOnlyList<string> EntityTypes { get; } = Array.AsReadOnly(new[] { "core.business_capability", "core.application", "core.data_asset" });
    public static IReadOnlyList<string> RelationshipTypes { get; } = Array.AsReadOnly(new[] { "core.supports", "core.uses" });
    public static void RequireRevision(int actual, int expected)
    {
        if (actual != expected) throw new CatalogFault(CatalogFailure.Revision, "CAT_REVISION_CONFLICT", "El registro cambió; recarga y revisa tus cambios");
    }
    public static void RequireActive(string status)
    {
        if (status != "active") throw new CatalogFault(CatalogFailure.Conflict, "CAT_ARCHIVED", "El registro está archivado");
    }
    public static string Text(string text, int max, bool trim = false)
    {
        if (trim) text = text.Trim();
        var length = 0;
        for (var offset = 0; offset < text.Length;)
        {
            if (Rune.DecodeFromUtf16(text.AsSpan(offset), out var rune, out var consumed) != OperationStatus.Done || rune.Value == 0)
                throw Invalid("Texto Unicode inválido o no representable");
            offset += consumed; length++;
        }
        if (length > max || (trim && string.IsNullOrWhiteSpace(text))) throw Invalid("Texto inválido");
        return text;
    }
    public static CatalogFault Invalid(string message) => new(CatalogFailure.Validation, "CAT_VALIDATION", message);
}
public sealed class ArchitectureEntity
{
    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public string TypeId { get; }
    public string SchemaVersion { get; }
    public string Code { get; }
    public string Name { get; }
    public string? Description { get; }
    public string Classification { get; }
    public string Status { get; }
    public int Revision { get; }
    private ArchitectureEntity(Guid id, Guid org, string type, string schema, string code, string name, string? description, string classification, string status, int revision)
    { Id = id; OrganizationId = org; TypeId = type; SchemaVersion = schema; Code = code; Name = name; Description = description; Classification = classification; Status = status; Revision = revision; }
    public static ArchitectureEntity Create(Guid id, Guid org, string type, string schema, string code, string name, string? description, string classification)
    {
        if (!CatalogDefinitions.EntityTypes.Contains(type) || schema != CatalogDefinitions.SchemaVersion) throw CatalogDefinitions.Invalid("Tipo o versión no admitidos");
        var inputCode = CatalogDefinitions.Text(code, 64, true);
        if (!Regex.IsMatch(inputCode, "^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.NonBacktracking)) throw CatalogDefinitions.Invalid("Código inválido");
        var normalized = inputCode.ToUpperInvariant();
        var validName = CatalogDefinitions.Text(name, 200, true);
        if (description is not null) CatalogDefinitions.Text(description, 4000);
        if (!new[] { "internal", "confidential", "restricted" }.Contains(classification)) throw CatalogDefinitions.Invalid("Clasificación inválida");
        return new(id, org, type, schema, normalized, validName, description, classification, "active", 1);
    }
    public static ArchitectureEntity Restore(Guid id, Guid org, string type, string schema, string code, string name, string? description, string classification, string status, int revision)
    {
        var valid = Create(id, org, type, schema, code, name, description, classification);
        if (revision < 1 || !new[] { "active", "archived" }.Contains(status)) throw CatalogDefinitions.Invalid("Estado persistido inválido");
        return new(id, org, type, schema, valid.Code, valid.Name, description, classification, status, revision);
    }
    public ArchitectureEntity Edit(string name, string? description, string classification, int expected)
    {
        CatalogDefinitions.RequireRevision(Revision, expected); CatalogDefinitions.RequireActive(Status);
        var valid = Create(Id, OrganizationId, TypeId, SchemaVersion, Code, name, description, classification);
        return new(Id, OrganizationId, TypeId, SchemaVersion, Code, valid.Name, description, classification, Status, Revision + 1);
    }
    public ArchitectureEntity Archive(int activeRelationships, int expected)
    {
        CatalogDefinitions.RequireRevision(Revision, expected); CatalogDefinitions.RequireActive(Status);
        if (activeRelationships > 0) throw new CatalogFault(CatalogFailure.Conflict, "CAT_ACTIVE_RELATIONSHIPS", "Archiva primero las relaciones activas");
        return new(Id, OrganizationId, TypeId, SchemaVersion, Code, Name, Description, Classification, "archived", Revision + 1);
    }
}
public sealed class ArchitectureRelationship
{
    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public string TypeId { get; }
    public string SchemaVersion { get; }
    public Guid SourceId { get; }
    public Guid TargetId { get; }
    private ArchitectureRelationship(Guid id, Guid org, string type, string schema, Guid source, Guid target)
    { Id = id; OrganizationId = org; TypeId = type; SchemaVersion = schema; SourceId = source; TargetId = target; }
    public static ArchitectureRelationship Create(Guid id, Guid org, string type, string schema, ArchitectureEntity source, ArchitectureEntity target)
    {
        if (source.OrganizationId != org || target.OrganizationId != org) throw new CatalogFault(CatalogFailure.NotFound, "CAT_NOT_FOUND", "Recurso no encontrado");
        if (source.Id == target.Id) throw CatalogDefinitions.Invalid("Los extremos deben ser diferentes");
        if (!CatalogDefinitions.RelationshipTypes.Contains(type) || schema != CatalogDefinitions.SchemaVersion) throw CatalogDefinitions.Invalid("Tipo o versión no admitidos");
        if (source.Status != "active" || target.Status != "active") throw new CatalogFault(CatalogFailure.Conflict, "CAT_ARCHIVED_ENDPOINT", "El extremo está archivado");
        var expected = type == "core.supports" ? "core.business_capability" : "core.data_asset";
        if (source.TypeId != "core.application" || target.TypeId != expected) throw CatalogDefinitions.Invalid("Tipos de extremos incompatibles");
        return new(id, org, type, schema, source.Id, target.Id);
    }
}
