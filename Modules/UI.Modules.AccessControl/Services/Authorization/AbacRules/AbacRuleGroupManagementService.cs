using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using UI.Modules.AccessControl.Models;
using Microsoft.Extensions.Logging;

namespace UI.Modules.AccessControl.Services.Authorization.AbacRules;

/// <summary>
/// Service for managing ABAC rule groups with business logic.
/// Orchestrates repository calls, validation, and ViewModel mapping.
/// </summary>
public class AbacRuleGroupManagementService(
    IAbacRuleGroupRepository ruleGroupRepository,
    IResourceRepository resourceRepository,
    ILogger<AbacRuleGroupManagementService> logger) : IAbacRuleGroupManagementService
{
    private readonly IAbacRuleGroupRepository _ruleGroupRepository = ruleGroupRepository;
    private readonly IResourceRepository _resourceRepository = resourceRepository;
    private readonly ILogger<AbacRuleGroupManagementService> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<AbacRuleGroupViewModel>> GetRuleGroupsAsync(string workstream, string? search = null)
    {
        var groups = await _ruleGroupRepository.SearchAsync(workstream, search);

        return groups.Select(g => new AbacRuleGroupViewModel
        {
            Id = g.Id,
            WorkstreamId = g.WorkstreamId,
            GroupName = g.GroupName,
            Description = g.Description,
            ParentGroupId = g.ParentGroupId,
            ParentGroupName = g.ParentGroup?.GroupName,
            LogicalOperator = g.LogicalOperator,
            Resource = g.Resource,
            Action = g.Action,
            IsActive = g.IsActive,
            Priority = g.Priority,
            ChildGroupCount = g.ChildGroups.Count,
            RuleCount = g.Rules.Count
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<AbacRuleGroup?> GetRuleGroupByIdAsync(int id)
    {
        return await _ruleGroupRepository.GetByIdWithChildrenAsync(id);
    }

    /// <inheritdoc />
    public async Task<AbacRuleGroupViewModel?> GetRuleGroupViewModelByIdAsync(int id)
    {
        var group = await _ruleGroupRepository.GetByIdAsync(id);
        if (group == null)
        {
            return null;
        }

        return new AbacRuleGroupViewModel
        {
            Id = group.Id,
            WorkstreamId = group.WorkstreamId,
            GroupName = group.GroupName,
            Description = group.Description,
            ParentGroupId = group.ParentGroupId,
            LogicalOperator = group.LogicalOperator,
            Resource = group.Resource,
            Action = group.Action,
            IsActive = group.IsActive,
            Priority = group.Priority
        };
    }

    /// <inheritdoc />
    public async Task<(bool Success, AbacRuleGroup? RuleGroup, string? ErrorMessage)> CreateRuleGroupAsync(
        AbacRuleGroupViewModel model, string workstream)
    {
        var ruleGroup = new AbacRuleGroup
        {
            WorkstreamId = workstream,
            GroupName = model.GroupName,
            Description = model.Description,
            ParentGroupId = model.ParentGroupId,
            LogicalOperator = model.LogicalOperator,
            Resource = model.Resource,
            Action = model.Action,
            IsActive = model.IsActive,
            Priority = model.Priority
        };

        var created = await _ruleGroupRepository.CreateAsync(ruleGroup);
        return (true, created, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateRuleGroupAsync(
        int id, AbacRuleGroupViewModel model)
    {
        var ruleGroup = await _ruleGroupRepository.GetByIdAsync(id);
        if (ruleGroup == null)
        {
            return (false, "Rule group not found");
        }

        ruleGroup.GroupName = model.GroupName;
        ruleGroup.Description = model.Description;
        ruleGroup.ParentGroupId = model.ParentGroupId;
        ruleGroup.LogicalOperator = model.LogicalOperator;
        ruleGroup.Resource = model.Resource;
        ruleGroup.Action = model.Action;
        ruleGroup.IsActive = model.IsActive;
        ruleGroup.Priority = model.Priority;

        await _ruleGroupRepository.UpdateAsync(ruleGroup);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> DeleteRuleGroupAsync(int id)
    {
        var group = await _ruleGroupRepository.GetByIdWithChildrenAsync(id);
        if (group == null)
        {
            return (false, "Rule group not found");
        }

        // Business rule: Cannot delete groups with child groups or rules
        if (group.ChildGroups.Count > 0 || group.Rules.Count > 0)
        {
            return (false, "Cannot delete group with child groups or rules. Remove them first.");
        }

        await _ruleGroupRepository.DeleteAsync(id);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AbacRuleGroup>> GetParentGroupOptionsAsync(string workstream, int? excludeId = null)
    {
        return await _ruleGroupRepository.GetParentOptionsAsync(workstream, excludeId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableResourcesAsync(string workstream)
    {
        var resources = await _resourceRepository.SearchAsync(workstream, workstreamFilter: null, search: null);
        return resources.Select(r => r.ResourcePattern).ToList();
    }
}
