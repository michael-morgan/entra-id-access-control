namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Result of an authorization check.
/// </summary>
public record AuthorizationResult(
    bool IsAllowed,
    string? DenialReason = null);
