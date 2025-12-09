using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Authorization.Policies;

/// <summary>
/// Service interface for managing Casbin policies with business logic.
/// Orchestrates repository calls, Graph API lookups, and ViewModel mapping.
/// </summary>
public interface IPolicyManagementService
{
    /// <summary>
    /// Gets policies for a workstream with display names resolved from Graph API.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="policyType">Optional policy type filter (p, g, g2)</param>
    /// <param name="search">Optional search term</param>
    /// <returns>Tuple of (policies, subjectDisplayNames, availablePolicyTypes)</returns>
    Task<(IEnumerable<CasbinPolicy> Policies, Dictionary<string, string> SubjectDisplayNames, IEnumerable<string> PolicyTypes)>
        GetPoliciesWithDisplayNamesAsync(string workstream, string? policyType = null, string? search = null);

    /// <summary>
    /// Gets a single policy by ID with subject display name resolved.
    /// </summary>
    /// <param name="id">The policy ID</param>
    /// <returns>Tuple of (policy, subjectDisplayNames) or null if not found</returns>
    Task<(CasbinPolicy? Policy, Dictionary<string, string> SubjectDisplayNames)?>
        GetPolicyByIdWithDisplayNamesAsync(int id);

    /// <summary>
    /// Creates a new policy.
    /// </summary>
    /// <param name="model">The policy view model</param>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="createdBy">The user creating the policy</param>
    /// <returns>The created policy</returns>
    Task<CasbinPolicy> CreatePolicyAsync(PolicyViewModel model, string workstream, string createdBy);

    /// <summary>
    /// Updates an existing policy.
    /// </summary>
    /// <param name="id">The policy ID</param>
    /// <param name="model">The updated policy view model</param>
    /// <param name="modifiedBy">The user modifying the policy</param>
    /// <returns>True if successful, false if policy not found or validation failed</returns>
    Task<bool> UpdatePolicyAsync(int id, PolicyViewModel model, string modifiedBy);

    /// <summary>
    /// Deletes a policy.
    /// </summary>
    /// <param name="id">The policy ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeletePolicyAsync(int id);

    /// <summary>
    /// Checks if a policy exists.
    /// </summary>
    /// <param name="id">The policy ID</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> PolicyExistsAsync(int id);

    /// <summary>
    /// Gets available role names for a workstream (for dropdown lists).
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>List of active role names</returns>
    Task<IEnumerable<string>> GetAvailableRoleNamesAsync(string workstream);

    /// <summary>
    /// Gets available resource patterns for a workstream (for dropdown lists).
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>List of resource patterns (includes global resources)</returns>
    Task<IEnumerable<string>> GetAvailableResourcePatternsAsync(string workstream);

    /// <summary>
    /// Gets all available workstreams (for dropdown lists).
    /// </summary>
    /// <returns>List of distinct workstream IDs</returns>
    Task<IEnumerable<string>> GetAvailableWorkstreamsAsync();
}
