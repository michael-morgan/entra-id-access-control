namespace UI.Modules.AccessControl.Models;

public class TokenAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Token Claims
    public Dictionary<string, string> Claims { get; set; } = new();
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public List<string> Groups { get; set; } = new();
    public List<string> Roles { get; set; } = new();

    // ABAC Attributes
    public List<AttributeViewModel> UserAttributes { get; set; } = new();
    public List<AttributeViewModel> RoleAttributes { get; set; } = new();
    public List<AttributeViewModel> GroupAttributes { get; set; } = new();

    // Merged Attributes (with precedence applied)
    public Dictionary<string, AttributeValue> MergedAttributes { get; set; } = new();

    // RBAC Policies
    public List<PolicySummary> ApplicablePolicies { get; set; } = new();

    // ABAC Rules
    public List<AbacRuleSummary> ApplicableAbacRules { get; set; } = new();
    public List<AbacRuleGroupSummary> ApplicableRuleGroups { get; set; } = new();
}

public class AttributeViewModel
{
    public string AttributeName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "User", "Role", "Group"
    public string? EntityId { get; set; } // The specific user/role/group ID
}

public class AttributeValue
{
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "User", "Role", "Group"
    public string? EntityId { get; set; }
}

public class PolicySummary
{
    public int Id { get; set; }
    public string PolicyType { get; set; } = string.Empty;
    public string V0 { get; set; } = string.Empty;
    public string? V1 { get; set; }
    public string? V2 { get; set; }
    public string? V3 { get; set; }
    public string? V4 { get; set; }
    public string? V5 { get; set; }
    public string WorkstreamId { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
}

public class AbacRuleSummary
{
    public int Id { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string? ComparisonValue { get; set; }
    public string? EntityAttribute { get; set; }
    public string WorkstreamId { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
}

public class AbacRuleGroupSummary
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string LogicOperator { get; set; } = string.Empty;
    public int RuleCount { get; set; }
    public string WorkstreamId { get; set; } = string.Empty;
}
