using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Service for evaluating ABAC rules.
/// Replaces static service locator pattern in CasbinAbacFunctions.
/// </summary>
public class AbacEvaluationService(
    IWorkstreamAbacEvaluatorRegistry? evaluatorRegistry,
    IServiceProvider serviceProvider,
    ILogger<AbacEvaluationService> logger) : IAbacEvaluationService
{
    private readonly IWorkstreamAbacEvaluatorRegistry? _evaluatorRegistry = evaluatorRegistry;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
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
            var context = JsonSerializer.Deserialize<AbacContext>(contextJson)?.EnsureCaseInsensitiveDictionaries();
            if (context == null)
            {
                _logger.LogDebug("Deserialized context is null, allowing request");
                return true;
            }

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
            var context = JsonSerializer.Deserialize<AbacContext>(contextJson)?.EnsureCaseInsensitiveDictionaries();
            if (context == null)
            {
                _logger.LogDebug("Deserialized context is null, allowing request");
                return true;
            }

            // Phase 1: Check custom workstream-specific evaluators (e.g., LoansAbacEvaluator)
            if (_evaluatorRegistry != null)
            {
                // Synchronously wait for async evaluation (Casbin custom functions must be synchronous)
                // Use GetAwaiter().GetResult() instead of .Wait() to avoid deadlocks
                var result = _evaluatorRegistry.EvaluateAsync(workstream, context, resource, action)
                    .GetAwaiter()
                    .GetResult();

                if (result != null)
                {
                    _logger.LogDebug("Custom evaluator result - Allowed: {Allowed}, Reason: {Reason}",
                        result.Allowed, result.Reason);

                    if (!result.Allowed)
                    {
                        _logger.LogInformation("Access denied by custom ABAC evaluator: {Reason}", result.Reason);
                        return false; // Explicit deny from custom evaluator
                    }
                }
                else
                {
                    _logger.LogDebug("No custom evaluator handled this request");
                }
            }
            else
            {
                _logger.LogDebug("No evaluator registry found");
            }

            // Phase 2: Check declarative ABAC rules from database (GenericAbacEvaluator)
            // Create a scope to resolve scoped dependencies (AccessControlDbContext)
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
                    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

                    var genericEvaluator = new GenericAbacEvaluator(
                        workstream,
                        dbContext,
                        loggerFactory.CreateLogger<GenericAbacEvaluator>());

                    // Synchronously evaluate generic rules
                    var genericResult = genericEvaluator.EvaluateAsync(context, resource, action)
                        .GetAwaiter()
                        .GetResult();

                    if (genericResult != null)
                    {
                        _logger.LogDebug("Generic evaluator result - Allowed: {Allowed}, Reason: {Reason}",
                            genericResult.Allowed, genericResult.Reason);

                        if (!genericResult.Allowed)
                        {
                            _logger.LogInformation("Access denied by generic ABAC rules: {Reason}", genericResult.Reason);
                            return false; // Explicit deny from generic rules
                        }
                    }
                    else
                    {
                        _logger.LogDebug("No generic ABAC rules applied");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating generic ABAC rules");
                    // On error, deny for safety
                    return false;
                }
            }

            _logger.LogDebug("All ABAC rules passed (custom + generic), allowing request");
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
