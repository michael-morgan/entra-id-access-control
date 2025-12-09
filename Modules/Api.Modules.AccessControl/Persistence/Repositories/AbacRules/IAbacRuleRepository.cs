using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.AbacRules;

/// <summary>
/// Repository interface for managing ABAC rules.
/// Provides data access abstraction for rule CRUD operations.
/// </summary>
public interface IAbacRuleRepository
{
    /// <summary>
    /// Gets all rules for a specific workstream with optional filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term for RuleName</param>
    /// <param name="ruleType">Optional rule type filter</param>
    /// <param name="ruleGroupId">Optional rule group ID filter</param>
    /// <returns>Collection of rules matching the criteria</returns>
    Task<IEnumerable<AbacRule>> SearchAsync(string workstream, string? search = null, string? ruleType = null, int? ruleGroupId = null);

    /// <summary>
    /// Gets a single rule by ID with rule group included.
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <returns>The rule if found, null otherwise</returns>
    Task<AbacRule?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new rule.
    /// </summary>
    /// <param name="rule">The rule to create</param>
    /// <returns>The created rule with ID populated</returns>
    Task<AbacRule> CreateAsync(AbacRule rule);

    /// <summary>
    /// Updates an existing rule.
    /// </summary>
    /// <param name="rule">The rule to update</param>
    Task UpdateAsync(AbacRule rule);

    /// <summary>
    /// Deletes a rule by ID.
    /// </summary>
    /// <param name="id">The rule ID to delete</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Checks if a rule exists.
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(int id);
}
