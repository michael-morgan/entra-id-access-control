using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;

namespace UI.Modules.AccessControl.Services.Authorization.AbacRules;

public class AbacRuleManagementService(
    IAbacRuleRepository ruleRepository,
    IAbacRuleGroupRepository ruleGroupRepository) : IAbacRuleManagementService
{
    private readonly IAbacRuleRepository _ruleRepository = ruleRepository;
    private readonly IAbacRuleGroupRepository _ruleGroupRepository = ruleGroupRepository;

    public async Task<IEnumerable<AbacRule>> GetRulesAsync(string workstream, string? search = null, string? ruleType = null, int? ruleGroupId = null)
    {
        return await _ruleRepository.SearchAsync(workstream, search, ruleType, ruleGroupId);
    }

    public async Task<AbacRule?> GetRuleByIdAsync(int id)
    {
        return await _ruleRepository.GetByIdAsync(id);
    }

    public async Task<(bool Success, AbacRule? Rule, string? ErrorMessage)> CreateRuleAsync(AbacRule rule, string workstream, string createdBy)
    {
        rule.WorkstreamId = workstream;
        rule.CreatedBy = createdBy;
        var created = await _ruleRepository.CreateAsync(rule);
        return (true, created, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateRuleAsync(int id, AbacRule rule, string modifiedBy)
    {
        var existing = await _ruleRepository.GetByIdAsync(id);
        if (existing == null) return (false, "Rule not found");

        rule.ModifiedBy = modifiedBy;
        await _ruleRepository.UpdateAsync(rule);
        return (true, null);
    }

    public async Task<bool> DeleteRuleAsync(int id)
    {
        var rule = await _ruleRepository.GetByIdAsync(id);
        if (rule == null) return false;

        await _ruleRepository.DeleteAsync(id);
        return true;
    }

    public async Task<IEnumerable<AbacRuleGroup>> GetRuleGroupsForWorkstreamAsync(string workstream)
    {
        return await _ruleGroupRepository.GetParentOptionsAsync(workstream);
    }
}
