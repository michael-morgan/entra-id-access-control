using Api.Modules.AccessControl.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// ABAC evaluation functions for Casbin.
/// These are static bridge methods required by Casbin's custom function API.
/// The actual logic is delegated to IAbacEvaluationService for better testability and maintainability.
///
/// NOTE: Casbin requires custom functions to be static methods, which necessitates
/// keeping a reference to the service provider. The Service Locator pattern is
/// isolated to this thin bridge layer only.
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
    ///
    /// This is a thin bridge to IAbacEvaluationService.EvaluateContext.
    /// </summary>
    public static bool EvalContext(string contextJson, string subject, string workstream, string resource, string action)
    {
        if (_serviceProvider == null)
        {
            // Fail-safe: allow if DI not configured (should never happen in production)
            return true;
        }

        using var scope = _serviceProvider.CreateScope();
        var evaluationService = scope.ServiceProvider.GetService<IAbacEvaluationService>();

        if (evaluationService == null)
        {
            // Fail-safe: allow if service not registered
            return true;
        }

        return evaluationService.EvaluateContext(contextJson, subject, workstream, resource, action);
    }

    /// <summary>
    /// Evaluates ABAC rules (declarative rules from database and IWorkstreamAbacEvaluator implementations).
    /// Called from Casbin matcher with: evalAbacRules(r.ctx, r.workstream, r.res, r.act)
    ///
    /// This is a thin bridge to IAbacEvaluationService.EvaluateAbacRules.
    /// Returns false if any rule denies access, true if all pass or no rules exist.
    /// </summary>
    public static bool EvalAbacRules(string contextJson, string workstream, string resource, string action)
    {
        if (_serviceProvider == null)
        {
            // Fail-safe: allow if DI not configured (should never happen in production)
            return true;
        }

        using var scope = _serviceProvider.CreateScope();
        var evaluationService = scope.ServiceProvider.GetService<IAbacEvaluationService>();

        if (evaluationService == null)
        {
            // Fail-safe: allow if service not registered
            return true;
        }

        return evaluationService.EvaluateAbacRules(contextJson, workstream, resource, action);
    }
}
