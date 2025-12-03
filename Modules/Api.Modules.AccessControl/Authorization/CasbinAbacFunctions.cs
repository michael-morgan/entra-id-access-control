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

            Console.WriteLine($"[ABAC DEBUG] Context: Region={context.Region}, Dept={context.Department}");

            // ═══════════════════════════════════════════════════════════
            // WORKSTREAM-SPECIFIC ABAC RULES
            // ═══════════════════════════════════════════════════════════

            if (workstream == "loans")
            {
                var result = EvaluateLoansAbac(context, resource, action);
                Console.WriteLine($"[ABAC DEBUG] EvaluateLoansAbac returned: {result}");
                return result;
            }
            else if (workstream == "claims")
            {
                return EvaluateClaimsAbac(context, resource, action);
            }
            else if (workstream == "documents")
            {
                return EvaluateDocumentsAbac(context, resource, action);
            }

            // Default: allow if no specific ABAC rules
            Console.WriteLine("[ABAC DEBUG] No specific workstream, returning true");
            return true;
        }
        catch (Exception ex)
        {
            // On error, deny access
            Console.WriteLine($"[ABAC DEBUG] Exception: {ex.Message}");
            return false;
        }
    }

    private static bool EvaluateLoansAbac(AbacContext context, string resource, string action)
    {
        // RULE: Approval requires sufficient approval limit
        if (action == "approve")
        {
            if (!context.ApprovalLimit.HasValue)
                return false;

            if (context.ResourceValue.HasValue)
            {
                // User's approval limit must be >= loan amount
                if (context.ApprovalLimit.Value < context.ResourceValue.Value)
                    return false;
            }
        }

        // RULE: Regional access - users can only access loans in their region
        if (context.Region != null && context.ResourceRegion != null)
        {
            if (context.Region != context.ResourceRegion && context.Region != "ALL")
                return false;
        }

        // RULE: Ownership - users can always access their own loans
        if (context.ResourceOwnerId != null && context.UserId == context.ResourceOwnerId)
            return true;

        // RULE: Status-based access
        if (resource.StartsWith("Loan/") && action == "write")
        {
            // Can only modify loans in Draft or UnderReview status
            if (context.ResourceStatus != null &&
                context.ResourceStatus != "Draft" &&
                context.ResourceStatus != "UnderReview")
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateClaimsAbac(AbacContext context, string resource, string action)
    {
        // RULE: High-value claims require manager approval
        if (action == "approve" && context.ResourceValue.HasValue)
        {
            decimal highValueThreshold = 50000m;

            if (context.ResourceValue.Value > highValueThreshold)
            {
                // Requires management level 2 or higher
                if (!context.ManagementLevel.HasValue || context.ManagementLevel.Value < 2)
                    return false;
            }
        }

        // RULE: Regional access
        if (context.Region != null && context.ResourceRegion != null)
        {
            if (context.Region != context.ResourceRegion && context.Region != "ALL")
                return false;
        }

        // RULE: Sensitive claims require internal network
        if (context.ResourceClassification == "Sensitive")
        {
            if (!context.IsInternalNetwork)
                return false;
        }

        return true;
    }

    private static bool EvaluateDocumentsAbac(AbacContext context, string resource, string action)
    {
        // RULE: Confidential documents - only during business hours
        if (context.ResourceClassification == "Confidential")
        {
            if (action == "read" || action == "write")
            {
                if (!context.IsBusinessHours)
                    return false;
            }
        }

        // RULE: Department-scoped documents
        if (context.ResourceClassification == "Departmental")
        {
            if (context.Department != null && context.Department != "Executive")
            {
                // Check custom attributes for document department
                if (context.CustomAttributes.TryGetValue("documentDepartment", out var docDept))
                {
                    if (docDept?.ToString() != context.Department)
                        return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Evaluates ABAC rules (both code-based evaluators and declarative rules from database).
    /// Called from Casbin matcher with: evalAbacRules(r.ctx, r.workstream, r.res, r.act)
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
