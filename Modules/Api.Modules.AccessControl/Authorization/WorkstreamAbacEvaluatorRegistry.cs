using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Registry for managing workstream-specific ABAC evaluators.
/// Supports multiple evaluators per workstream and delegates evaluation to registered evaluators.
/// </summary>
public class WorkstreamAbacEvaluatorRegistry : IWorkstreamAbacEvaluatorRegistry
{
    private readonly Dictionary<string, List<IWorkstreamAbacEvaluator>> _evaluators = [];
    private readonly ILogger<WorkstreamAbacEvaluatorRegistry> _logger;

    public WorkstreamAbacEvaluatorRegistry(
        IEnumerable<IWorkstreamAbacEvaluator> evaluators,
        ILogger<WorkstreamAbacEvaluatorRegistry> logger)
    {
        _logger = logger;

        // Group evaluators by workstream
        foreach (var evaluator in evaluators)
        {
            if (!_evaluators.TryGetValue(evaluator.WorkstreamId, out List<IWorkstreamAbacEvaluator>? value))
            {
                value = [];
                _evaluators[evaluator.WorkstreamId] = value;
            }

            value.Add(evaluator);
            _logger.LogInformation(
                "Registered ABAC evaluator {EvaluatorType} for workstream {WorkstreamId}",
                evaluator.GetType().Name, evaluator.WorkstreamId);
        }
    }

    public async Task<AbacEvaluationResult?> EvaluateAsync(
        string workstreamId,
        AbacContext context,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (!_evaluators.TryGetValue(workstreamId, out var workstreamEvaluators))
        {
            _logger.LogDebug("No ABAC evaluators registered for workstream {WorkstreamId}", workstreamId);
            return null;
        }

        // Try each evaluator until one handles the request
        foreach (var evaluator in workstreamEvaluators)
        {
            try
            {
                var result = await evaluator.EvaluateAsync(context, resource, action, cancellationToken);

                if (result != null)
                {
                    _logger.LogInformation(
                        "ABAC evaluator {EvaluatorType} handled {Resource}:{Action} for workstream {WorkstreamId}. Result: {Allowed}",
                        evaluator.GetType().Name, resource, action, workstreamId, result.Allowed);

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in ABAC evaluator {EvaluatorType} for {Resource}:{Action} in workstream {WorkstreamId}",
                    evaluator.GetType().Name, resource, action, workstreamId);

                // Continue to next evaluator on error
            }
        }

        _logger.LogDebug(
            "No ABAC evaluator handled {Resource}:{Action} for workstream {WorkstreamId}",
            resource, action, workstreamId);

        return null;
    }

    public IReadOnlyList<IWorkstreamAbacEvaluator> GetEvaluators(string workstreamId)
    {
        return _evaluators.TryGetValue(workstreamId, out var evaluators)
            ? evaluators.AsReadOnly()
            : Array.Empty<IWorkstreamAbacEvaluator>();
    }
}
