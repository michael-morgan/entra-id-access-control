using System.Text.Json;

namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Runtime context for ABAC evaluation.
/// Built by IAbacContextProvider before authorization checks.
/// </summary>
public sealed record AbacContext
{
    // ═══════════════════════════════════════════════════════════
    // USER ATTRIBUTES (from JWT + enrichment)
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

    /// <summary>User's department (from directory or custom claim)</summary>
    public string? Department { get; init; }

    /// <summary>User's region/territory</summary>
    public string? Region { get; init; }

    /// <summary>Maximum amount user can approve</summary>
    public decimal? ApprovalLimit { get; init; }

    /// <summary>User's management level (for hierarchy)</summary>
    public int? ManagementLevel { get; init; }

    // ═══════════════════════════════════════════════════════════
    // RESOURCE ATTRIBUTES (from entity being accessed)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Owner of the resource</summary>
    public string? ResourceOwnerId { get; init; }

    /// <summary>Region the resource belongs to</summary>
    public string? ResourceRegion { get; init; }

    /// <summary>Current status of the resource</summary>
    public string? ResourceStatus { get; init; }

    /// <summary>Monetary value (for approval limits)</summary>
    public decimal? ResourceValue { get; init; }

    /// <summary>Sensitivity classification</summary>
    public string? ResourceClassification { get; init; }

    /// <summary>Date resource was created</summary>
    public DateTimeOffset? ResourceCreatedAt { get; init; }

    /// <summary>Dynamic resource attributes from entity properties</summary>
    public Dictionary<string, object> ResourceAttributes { get; init; } = new();

    // ═══════════════════════════════════════════════════════════
    // ENVIRONMENT ATTRIBUTES (runtime context)
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
    // EXTENSION POINT
    // ═══════════════════════════════════════════════════════════

    /// <summary>Custom attributes for workstream-specific ABAC</summary>
    public Dictionary<string, object> CustomAttributes { get; init; } = new();

    /// <summary>Serializes context to JSON for Casbin evaluation</summary>
    public string ToJson() => JsonSerializer.Serialize(this);
}
