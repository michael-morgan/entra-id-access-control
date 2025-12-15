using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Modules.AccessControl.Extensions;

namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Runtime context for ABAC evaluation.
/// Built by IAbacContextProvider before authorization checks.
/// Uses dynamic attribute dictionaries populated from AttributeSchemas and database JSON.
/// </summary>
public sealed record AbacContext
{
    // ═══════════════════════════════════════════════════════════
    // USER IDENTITY (from JWT)
    // ═══════════════════════════════════════════════════════════

    /// <summary>User's unique identifier (oid claim)</summary>
    public required string UserId { get; init; }

    /// <summary>User's display name</summary>
    public string? UserDisplayName { get; init; }

    /// <summary>User's email address</summary>
    public string? UserEmail { get; init; }

    /// <summary>Assigned app roles from Entra ID</summary>
    public required string[] Roles { get; init; }

    /// <summary>Group memberships from Entra ID</summary>
    public required string[] Groups { get; init; }

    // ═══════════════════════════════════════════════════════════
    // DYNAMIC USER ATTRIBUTES (from database JSON)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Dynamic user attributes loaded from UserAttributes/GroupAttributes/RoleAttributes tables.
    /// Merged with precedence: User > Role > Group.
    /// Examples: ApprovalLimit, ManagementLevel, Department, Region
    /// Uses case-insensitive key comparison for flexibility across serialization formats.
    /// </summary>
    public Dictionary<string, object> UserAttributes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    // ═══════════════════════════════════════════════════════════
    // DYNAMIC RESOURCE ATTRIBUTES (from entity properties)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Dynamic resource attributes extracted from entity via reflection.
    /// Examples: OwnerId, Region, Status, RequestedAmount, Classification
    /// Uses case-insensitive key comparison for flexibility across serialization formats.
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    // ═══════════════════════════════════════════════════════════
    // ENVIRONMENT ATTRIBUTES (computed at runtime)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Current request timestamp</summary>
    public DateTimeOffset RequestTime { get; init; }

    /// <summary>Client IP address</summary>
    public string? ClientIpAddress { get; init; }

    /// <summary>Whether request is during business hours</summary>
    public bool IsBusinessHours { get; init; }

    /// <summary>Whether request is from internal network</summary>
    public bool IsInternalNetwork { get; init; }

    // ═══════════════════════════════════════════════════════════
    // HELPER METHODS (type-safe access to dynamic attributes)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Gets a typed value from user attributes dictionary</summary>
    public T? GetUserAttribute<T>(string key) => UserAttributes.Get<T>(key);

    /// <summary>Gets a typed value from resource attributes dictionary</summary>
    public T? GetResourceAttribute<T>(string key) => ResourceAttributes.Get<T>(key);

    /// <summary>Serializes context to JSON for Casbin evaluation</summary>
    public string ToJson() => JsonSerializer.Serialize(this);

    /// <summary>
    /// Ensures attribute dictionaries are case-insensitive after JSON deserialization.
    /// Called after deserializing from JSON to restore case-insensitive behavior.
    /// </summary>
    public AbacContext EnsureCaseInsensitiveDictionaries()
    {
        // If dictionaries are already case-insensitive, return as-is
        if (UserAttributes.Comparer == StringComparer.OrdinalIgnoreCase &&
            ResourceAttributes.Comparer == StringComparer.OrdinalIgnoreCase)
        {
            return this;
        }

        // Recreate with case-insensitive comparer
        var newUserAttributes = new Dictionary<string, object>(UserAttributes, StringComparer.OrdinalIgnoreCase);
        var newResourceAttributes = new Dictionary<string, object>(ResourceAttributes, StringComparer.OrdinalIgnoreCase);

        return this with
        {
            UserAttributes = newUserAttributes,
            ResourceAttributes = newResourceAttributes
        };
    }
}
