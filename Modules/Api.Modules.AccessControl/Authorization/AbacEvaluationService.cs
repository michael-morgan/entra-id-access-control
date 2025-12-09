using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Service for evaluating ABAC rules.
/// Replaces static service locator pattern in CasbinAbacFunctions.
/// </summary>
public class AbacEvaluationService(
    IWorkstreamAbacEvaluatorRegistry? evaluatorRegistry,
    ILogger<AbacEvaluationService> logger) : IAbacEvaluationService
{
    private readonly IWorkstreamAbacEvaluatorRegistry? _evaluatorRegistry = evaluatorRegistry;
    private readonly ILogger<AbacEvaluationService> _logger = logger;

    /// <inheritdoc/>
    public bool EvaluateContext(string contextJson, string subject, string workstream, string resource, string action)
    {
        _logger.LogDebug("EvalContext called - Workstream: {Workstream}, Resource: {Resource}, Action: {Action}",
            workstream, resource, action);

        if (string.IsNullOrWhiteSpace(contextJson))
        {
            _logger.LogDebug("Context JSON is empty, allowing request");
            return true; // No ABAC rules, allow
        }

        try
        {
            var context = JsonSerializer.Deserialize<AbacContext>(contextJson);
            if (context == null)
            {
                _logger.LogDebug("Deserialized context is null, allowing request");
                return true;
            }

            var region = context.GetUserAttribute<string>("Region");
            var department = context.GetUserAttribute<string>("Department");
            _logger.LogDebug("ABAC context loaded - Region: {Region}, Department: {Department}",
                region, department);

            // Hardcoded workstream evaluators have been removed.
            // ABAC evaluation now handled by:
            // 1. IWorkstreamAbacEvaluator implementations (e.g., LoansAbacEvaluator)
            // 2. GenericAbacEvaluator with declarative AbacRules from database
            //
            // This function is now primarily used as a Casbin custom function
            // for backward compatibility, but business logic should use
            // IWorkstreamAbacEvaluatorRegistry instead.

            // Default: allow if no specific ABAC rules (delegate to evaluator registry)
            _logger.LogDebug("No hardcoded workstream rules, delegating to evaluator registry");
            return true;
        }
        catch (Exception ex)
        {
            // On error, deny access
            _logger.LogWarning(ex, "ABAC context evaluation failed, denying access");
            return false;
        }
    }

    /// <inheritdoc/>
    public bool EvaluateAbacRules(string contextJson, string workstream, string resource, string action)
    {
        _logger.LogDebug("EvalAbacRules called - Workstream: {Workstream}, Resource: {Resource}, Action: {Action}",
            workstream, resource, action);

        if (string.IsNullOrWhiteSpace(contextJson))
        {
            _logger.LogDebug("Context JSON is empty, allowing request");
            return true;
        }

        try
        {
            var context = JsonSerializer.Deserialize<AbacContext>(contextJson);
            if (context == null)
            {
                _logger.LogDebug("Deserialized context is null, allowing request");
                return true;
            }

            // Check code-based evaluators (IWorkstreamAbacEvaluator)
            if (_evaluatorRegistry != null)
            {
                // Synchronously wait for async evaluation (Casbin custom functions must be synchronous)
                // Use GetAwaiter().GetResult() instead of .Wait() to avoid deadlocks
                var result = _evaluatorRegistry.EvaluateAsync(workstream, context, resource, action)
                    .GetAwaiter()
                    .GetResult();

                if (result != null)
                {
                    _logger.LogDebug("Code-based evaluator result - Allowed: {Allowed}, Reason: {Reason}",
                        result.Allowed, result.Reason);

                    if (!result.Allowed)
                    {
                        _logger.LogInformation("Access denied by ABAC evaluator: {Reason}", result.Reason);
                        return false; // Explicit deny from code-based evaluator
                    }
                }
                else
                {
                    _logger.LogDebug("No code-based evaluator handled this request");
                }
            }
            else
            {
                _logger.LogDebug("No evaluator registry found");
            }

            // TODO: Check declarative ABAC rules from database (AbacRules table)
            // This will query the database for rules matching:
            // - WorkstreamId = workstream
            // - ResourcePattern matches resource
            // - Action matches action
            // - IsActive = true
            // Then evaluate each rule's RuleExpression against the context

            _logger.LogDebug("All ABAC rules passed, allowing request");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ABAC rules evaluation failed, denying access for safety");
            // On error, deny access for safety
            return false;
        }
    }
}
