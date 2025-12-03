using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Registry for managing and invoking workstream-specific ABAC evaluators.
/// </summary>
public interface IWorkstreamAbacEvaluatorRegistry
{
    /// <summary>
    /// Evaluates authorization by delegating to registered evaluators for the workstream.
    /// Returns null if no evaluator handles the request.
    /// </summary>
    Task<AbacEvaluationResult?> EvaluateAsync(
        string workstreamId,
        AbacContext context,
        string resource,
        string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all evaluators registered for a workstream.
    /// </summary>
    IReadOnlyList<IWorkstreamAbacEvaluator> GetEvaluators(string workstreamId);
}
