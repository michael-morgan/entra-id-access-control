using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// ABAC evaluation functions for Casbin.
/// </summary>
public static class CasbinAbacFunctions
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes the ABAC functions with DI container access.
    /// Called during startup configuration.
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    /// <summary>
    /// Evaluates ABAC context for authorization decisions.
    /// Called from Casbin matcher with: evalContext(r.ctx, p.sub, r.workstream, r.res, r.act)
    /// </summary>
    public static bool EvalContext(string contextJson, string subject, string workstream, string resource, string action)
    {
        Console.WriteLine($"[ABAC DEBUG] evalContext called: workstream={workstream}, resource={resource}, action={action}");

        if (string.IsNullOrWhiteSpace(contextJson))
        {
            Console.WriteLine("[ABAC DEBUG] contextJson is empty, returning true");
            return true; // No ABAC rules, allow
        }

        try
        {
            var context = JsonSerializer.Deserialize<AbacContext>(contextJson);
            if (context == null)
            {
                Console.WriteLine("[ABAC DEBUG] Deserialized context is null, returning true");
                return true;
            }

            var region = context.GetUserAttribute<string>("Region");
            var department = context.GetUserAttribute<string>("Department");
            Console.WriteLine($"[ABAC DEBUG] Context: Region={region}, Dept={department}");

            // ═══════════════════════════════════════════════════════════
            // WORKSTREAM-SPECIFIC ABAC RULES
            // ═══════════════════════════════════════════════════════════
            // NOTE: Hardcoded workstream evaluators have been removed.
            // ABAC evaluation now handled by:
            // 1. IWorkstreamAbacEvaluator implementations (e.g., LoansAbacEvaluator)
            // 2. GenericAbacEvaluator with declarative AbacRules from database
            //
            // This function is now primarily used as a Casbin custom function
            // for backward compatibility, but business logic should use
            // IWorkstreamAbacEvaluatorRegistry instead.

            // Default: allow if no specific ABAC rules (delegate to evaluator registry)
            Console.WriteLine("[ABAC DEBUG] No hardcoded workstream rules, returning true (delegate to evaluator registry)");
            return true;
        }
        catch (Exception ex)
        {
            // On error, deny access
            Console.WriteLine($"[ABAC DEBUG] Exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Evaluates ABAC rules (declarative rules from database and IWorkstreamAbacEvaluator implementations).
    /// Called from Casbin matcher with: evalAbacRules(r.ctx, r.workstream, r.res, r.act)
    ///
    /// NOTE: Hardcoded workstream-specific evaluators (EvaluateLoansAbac, EvaluateClaimsAbac, EvaluateDocumentsAbac)
    /// have been removed in favor of:
    /// 1. IWorkstreamAbacEvaluator implementations (e.g., LoansAbacEvaluator) for complex business logic
    /// 2. Declarative AbacRules stored in the database for simpler attribute checks
    ///
    /// Returns false if any rule denies access, true if all pass or no rules exist.
    /// </summary>
    public static bool EvalAbacRules(string contextJson, string workstream, string resource, string action)
    {
        Console.WriteLine($"[ABAC RULES DEBUG] evalAbacRules called: workstream={workstream}, resource={resource}, action={action}");

        if (_serviceProvider == null)
        {
            Console.WriteLine("[ABAC RULES DEBUG] ServiceProvider not initialized, returning true");
            return true; // No DI configured, allow
        }

        if (string.IsNullOrWhiteSpace(contextJson))
        {
            Console.WriteLine("[ABAC RULES DEBUG] contextJson is empty, returning true");
            return true;
        }

        try
        {
            var context = JsonSerializer.Deserialize<AbacContext>(contextJson);
            if (context == null)
            {
                Console.WriteLine("[ABAC RULES DEBUG] Deserialized context is null, returning true");
                return true;
            }

            // 1. Check code-based evaluators (IWorkstreamAbacEvaluator)
            using var scope = _serviceProvider.CreateScope();
            var evaluatorRegistry = scope.ServiceProvider.GetService<IWorkstreamAbacEvaluatorRegistry>();

            if (evaluatorRegistry != null)
            {
                // Synchronously wait for async evaluation (Casbin functions must be synchronous)
                var evaluationTask = evaluatorRegistry.EvaluateAsync(workstream, context, resource, action);
                evaluationTask.Wait();
                var result = evaluationTask.Result;

                if (result != null)
                {
                    Console.WriteLine($"[ABAC RULES DEBUG] Code-based evaluator returned: Allowed={result.Allowed}, Reason={result.Reason}");

                    if (!result.Allowed)
                    {
                        return false; // Explicit deny from code-based evaluator
                    }
                }
                else
                {
                    Console.WriteLine("[ABAC RULES DEBUG] No code-based evaluator handled this request");
                }
            }
            else
            {
                Console.WriteLine("[ABAC RULES DEBUG] No evaluator registry found");
            }

            // 2. TODO: Check declarative ABAC rules from database (AbacRules table)
            // This will query the database for rules matching:
            // - WorkstreamId = workstream
            // - ResourcePattern matches resource
            // - Action matches action
            // - IsActive = true
            // Then evaluate each rule's RuleExpression against the context

            Console.WriteLine("[ABAC RULES DEBUG] All ABAC rules passed, returning true");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ABAC RULES DEBUG] Exception: {ex.Message}");
            // On error, deny access for safety
            return false;
        }
    }
}
