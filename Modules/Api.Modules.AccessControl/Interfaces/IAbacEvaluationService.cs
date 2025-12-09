namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Service for evaluating ABAC (Attribute-Based Access Control) rules.
/// Provides a testable alternative to static Casbin custom functions.
/// </summary>
public interface IAbacEvaluationService
{
    /// <summary>
    /// Evaluates ABAC context for authorization decisions.
    /// </summary>
    /// <param name="contextJson">JSON-serialized ABAC context</param>
    /// <param name="subject">Subject (user/group ID)</param>
    /// <param name="workstream">Workstream identifier</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="action">Action being performed</param>
    /// <returns>True if access allowed, false otherwise</returns>
    bool EvaluateContext(string contextJson, string subject, string workstream, string resource, string action);

    /// <summary>
    /// Evaluates ABAC rules (both code-based evaluators and declarative database rules).
    /// </summary>
    /// <param name="contextJson">JSON-serialized ABAC context</param>
    /// <param name="workstream">Workstream identifier</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="action">Action being performed</param>
    /// <returns>True if all rules pass, false if any rule denies</returns>
    bool EvaluateAbacRules(string contextJson, string workstream, string resource, string action);
}
