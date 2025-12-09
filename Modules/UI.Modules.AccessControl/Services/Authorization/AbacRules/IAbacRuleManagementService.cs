using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace UI.Modules.AccessControl.Services.Authorization.AbacRules;

/// <summary>
/// Service interface for managing ABAC rules with business logic.
/// </summary>
public interface IAbacRuleManagementService
{
    Task<IEnumerable<AbacRule>> GetRulesAsync(string workstream, string? search = null, string? ruleType = null, int? ruleGroupId = null);
    Task<AbacRule?> GetRuleByIdAsync(int id);
    Task<(bool Success, AbacRule? Rule, string? ErrorMessage)> CreateRuleAsync(AbacRule rule, string workstream, string createdBy);
    Task<(bool Success, string? ErrorMessage)> UpdateRuleAsync(int id, AbacRule rule, string modifiedBy);
    Task<bool> DeleteRuleAsync(int id);
    Task<IEnumerable<AbacRuleGroup>> GetRuleGroupsForWorkstreamAsync(string workstream);
}
