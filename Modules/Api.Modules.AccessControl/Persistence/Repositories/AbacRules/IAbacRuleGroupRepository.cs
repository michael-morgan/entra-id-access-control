using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.AbacRules;

/// <summary>
/// Repository interface for managing ABAC rule groups.
/// Provides data access abstraction for rule group CRUD operations with hierarchical support.
/// </summary>
public interface IAbacRuleGroupRepository
{
    /// <summary>
    /// Gets all rule groups for a specific workstream with optional filtering and related entities.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term for GroupName or Description</param>
    /// <returns>Collection of rule groups with parent, children, and rules loaded</returns>
    Task<IEnumerable<AbacRuleGroup>> SearchAsync(string workstream, string? search = null);

    /// <summary>
    /// Gets a single rule group by ID.
    /// </summary>
    /// <param name="id">The rule group ID</param>
    /// <returns>The rule group if found, null otherwise</returns>
    Task<AbacRuleGroup?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a rule group by ID with all child groups and rules loaded.
    /// </summary>
    /// <param name="id">The rule group ID</param>
    /// <returns>The rule group with children and rules if found, null otherwise</returns>
    Task<AbacRuleGroup?> GetByIdWithChildrenAsync(int id);

    /// <summary>
    /// Creates a new rule group.
    /// </summary>
    /// <param name="ruleGroup">The rule group to create</param>
    /// <returns>The created rule group with ID populated</returns>
    Task<AbacRuleGroup> CreateAsync(AbacRuleGroup ruleGroup);

    /// <summary>
    /// Updates an existing rule group.
    /// </summary>
    /// <param name="ruleGroup">The rule group to update</param>
    Task UpdateAsync(AbacRuleGroup ruleGroup);

    /// <summary>
    /// Deletes a rule group by ID.
    /// </summary>
    /// <param name="id">The rule group ID to delete</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Gets all rule groups for a workstream that can be used as parent groups.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="excludeId">Optional rule group ID to exclude (prevents self-referencing)</param>
    /// <returns>Collection of rule groups suitable for parent selection</returns>
    Task<IEnumerable<AbacRuleGroup>> GetParentOptionsAsync(string workstream, int? excludeId = null);

    /// <summary>
    /// Checks if a rule group has child groups or rules.
    /// </summary>
    /// <param name="id">The rule group ID</param>
    /// <returns>True if the group has children or rules, false otherwise</returns>
    Task<bool> HasChildGroupsOrRulesAsync(int id);

    /// <summary>
    /// Checks if a rule group exists.
    /// </summary>
    /// <param name="id">The rule group ID</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(int id);
}
