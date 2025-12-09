using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Authorization.AbacRules;

/// <summary>
/// Service interface for managing ABAC rule groups with business logic.
/// Orchestrates repository calls, validation, and ViewModel mapping.
/// </summary>
public interface IAbacRuleGroupManagementService
{
    /// <summary>
    /// Gets all rule groups for a workstream with optional search, enriched as ViewModels.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term</param>
    /// <returns>Collection of rule group view models with child/rule counts</returns>
    Task<IEnumerable<AbacRuleGroupViewModel>> GetRuleGroupsAsync(string workstream, string? search = null);

    /// <summary>
    /// Gets a single rule group by ID with all related entities.
    /// </summary>
    /// <param name="id">The rule group ID</param>
    /// <returns>The rule group entity if found, null otherwise</returns>
    Task<AbacRuleGroup?> GetRuleGroupByIdAsync(int id);

    /// <summary>
    /// Gets a single rule group as ViewModel for editing.
    /// </summary>
    /// <param name="id">The rule group ID</param>
    /// <returns>The rule group view model if found, null otherwise</returns>
    Task<AbacRuleGroupViewModel?> GetRuleGroupViewModelByIdAsync(int id);

    /// <summary>
    /// Creates a new rule group with validation.
    /// </summary>
    /// <param name="model">The rule group view model</param>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>Tuple of (success, ruleGroup, errorMessage)</returns>
    Task<(bool Success, AbacRuleGroup? RuleGroup, string? ErrorMessage)> CreateRuleGroupAsync(
        AbacRuleGroupViewModel model, string workstream);

    /// <summary>
    /// Updates an existing rule group.
    /// </summary>
    /// <param name="id">The rule group ID</param>
    /// <param name="model">The updated rule group view model</param>
    /// <returns>Tuple of (success, errorMessage)</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateRuleGroupAsync(
        int id, AbacRuleGroupViewModel model);

    /// <summary>
    /// Deletes a rule group with validation (prevents deletion of groups with children/rules).
    /// </summary>
    /// <param name="id">The rule group ID to delete</param>
    /// <returns>Tuple of (success, errorMessage)</returns>
    Task<(bool Success, string? ErrorMessage)> DeleteRuleGroupAsync(int id);

    /// <summary>
    /// Gets available parent group options for dropdown lists.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="excludeId">Optional ID to exclude (prevents self-referencing)</param>
    /// <returns>Collection of rule groups suitable as parent options</returns>
    Task<IEnumerable<AbacRuleGroup>> GetParentGroupOptionsAsync(string workstream, int? excludeId = null);

    /// <summary>
    /// Gets available resources for dropdown lists.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>Collection of resource patterns</returns>
    Task<IEnumerable<string>> GetAvailableResourcesAsync(string workstream);
}
