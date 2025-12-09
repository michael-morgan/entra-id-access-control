using System.Text.Json;

namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Group attributes for ABAC from database with dynamic JSON attributes.
/// </summary>
public sealed record GroupAttributes
{
    public required string GroupId { get; init; }
    public required string WorkstreamId { get; init; }
    public string? GroupName { get; init; }
    public Dictionary<string, JsonElement> Attributes { get; init; } = [];

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

        return [.. value.EnumerateArray()
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!)];
    }
}
