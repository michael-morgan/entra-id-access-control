using System.Text.Json;

namespace Api.Modules.AccessControl.Models;

/// <summary>
/// User attributes for ABAC from database with dynamic JSON attributes.
/// These override group and role attributes (highest precedence).
/// </summary>
public sealed record UserAttributes
{
    public required string UserId { get; init; }
    public required string WorkstreamId { get; init; }
    public Dictionary<string, JsonElement> Attributes { get; init; } = new();

    /// <summary>
    /// Get string attribute value.
    /// </summary>
    public string? GetString(string key) =>
        Attributes.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// Get decimal attribute value.
    /// </summary>
    public decimal? GetDecimal(string key) =>
        Attributes.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal()
            : null;

    /// <summary>
    /// Get integer attribute value.
    /// </summary>
    public int? GetInt(string key) =>
        Attributes.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    /// <summary>
    /// Get boolean attribute value.
    /// </summary>
    public bool? GetBoolean(string key) =>
        Attributes.TryGetValue(key, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            ? value.GetBoolean()
            : null;

    /// <summary>
    /// Get string array attribute value.
    /// </summary>
    public string[]? GetArray(string key)
    {
        if (!Attributes.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Array)
            return null;

        return value.EnumerateArray()
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!)
            .ToArray();
    }
}
