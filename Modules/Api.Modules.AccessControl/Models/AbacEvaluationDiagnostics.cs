namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Diagnostic information about ABAC rule evaluation.
/// Helps identify why authorization decisions were made and troubleshoot misconfigurations.
/// </summary>
public class AbacEvaluationDiagnostics
{
    /// <summary>
    /// Rule groups that matched the resource/action pattern
    /// </summary>
    public List<RuleGroupMatch> MatchedRuleGroups { get; set; } = [];

    /// <summary>
    /// Rules that were evaluated during this authorization check
    /// </summary>
    public List<RuleEvaluation> EvaluatedRules { get; set; } = [];

    /// <summary>
    /// Required attributes that are not validated by the current action's rule group
    /// </summary>
    public List<MissingValidation> MissingValidations { get; set; } = [];

    /// <summary>
    /// Actionable suggestions for fixing configuration issues
    /// </summary>
    public List<string> Suggestions { get; set; } = [];
}

/// <summary>
/// Information about a rule group that matched the request
/// </summary>
public class RuleGroupMatch
{
    public string GroupName { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int RuleCount { get; set; }
}

/// <summary>
/// Information about a rule that was evaluated
/// </summary>
public class RuleEvaluation
{
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Information about a required attribute that is not being validated
/// </summary>
public class MissingValidation
{
    /// <summary>
    /// Name of the attribute that should be validated
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// Where the attribute should come from (User, Resource, Environment)
    /// </summary>
    public string AttributeSource { get; set; } = string.Empty;

    /// <summary>
    /// Why this validation is needed (e.g., "Required by schema", "Common pattern for this action")
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Severity level: High (required attribute), Medium (common pattern), Low (suggestion)
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Recommended rule type to add (e.g., "PropertyMatch", "AttributeComparison")
    /// </summary>
    public string? RecommendedRuleType { get; set; }

    /// <summary>
    /// Whether this validation exists in other action groups for cross-reference
    /// </summary>
    public List<string> ExistsInActions { get; set; } = [];
}
