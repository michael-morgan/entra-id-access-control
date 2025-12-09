namespace UI.Modules.AccessControl.Models;

public class TokenAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Token Claims
    public Dictionary<string, string> Claims { get; set; } = [];
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public List<string> Groups { get; set; } = []; // Group OIDs (for backward compatibility)
    public List<GroupReference> GroupsWithNames { get; set; } = []; // Groups with friendly names
    public List<string> Roles { get; set; } = [];

    // ABAC Attributes
    public List<AttributeViewModel> UserAttributes { get; set; } = [];
    public List<AttributeViewModel> RoleAttributes { get; set; } = [];
    public List<AttributeViewModel> GroupAttributes { get; set; } = [];

    // Merged Attributes (with precedence applied)
    public Dictionary<string, AttributeValue> MergedAttributes { get; set; } = [];

    // RBAC Policies
    public List<PolicySummary> ApplicablePolicies { get; set; } = [];

    // ABAC Rules
    public List<AbacRuleSummary> ApplicableAbacRules { get; set; } = [];
    public List<AbacRuleGroupSummary> ApplicableRuleGroups { get; set; } = [];
}

public class AttributeViewModel
{
    public string AttributeName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "User", "Role", "Group"
    public string? EntityId { get; set; } // The specific user/role/group ID
    public string? EntityName { get; set; } // Friendly name for the entity (user display name, group name, role name)
}

public class AttributeValue
{
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "User", "Role", "Group"
    public string? EntityId { get; set; }
    public string? EntityName { get; set; } // Friendly name for the entity
}

public class GroupReference
{
    public required string Id { get; init; } // Group OID
    public required string DisplayName { get; init; } // Friendly group name
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
