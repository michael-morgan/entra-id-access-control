using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Custom ABAC evaluator for workstream-specific authorization logic.
/// Handles the 10% of complex rules that cannot be expressed declaratively.
/// </summary>
public interface IWorkstreamAbacEvaluator
{
    /// <summary>
    /// The workstream this evaluator is registered for.
    /// </summary>
    string WorkstreamId { get; }

    /// <summary>
    /// Evaluates authorization for a specific resource and action.
    /// Returns null if this evaluator doesn't handle this resource/action combination.
    /// </summary>
    /// <param name="context">Runtime ABAC context with user, resource, and environment attributes</param>
    /// <param name="resource">Resource identifier (e.g., "loans", "invoices")</param>
    /// <param name="action">Action being performed (e.g., "approve", "view")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result, or null if not handled by this evaluator</returns>
    Task<AbacEvaluationResult?> EvaluateAsync(
        AbacContext context,
        string resource,
        string action,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an ABAC evaluation.
/// </summary>
public record AbacEvaluationResult
{
    /// <summary>
    /// Whether access is granted.
    /// </summary>
    public required bool Allowed { get; init; }

    /// <summary>
    /// Reason for the decision (for logging/debugging).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// User-friendly message (for UI display).
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates an allowed result.
    /// </summary>
    public static AbacEvaluationResult Allow(string? reason = null) => new()
    {
        Allowed = true,
        Reason = reason
    };

    /// <summary>
    /// Creates a denied result.
    /// </summary>
    public static AbacEvaluationResult Deny(string reason, string? message = null) => new()
    {
        Allowed = false,
        Reason = reason,
        Message = message
    };
}
