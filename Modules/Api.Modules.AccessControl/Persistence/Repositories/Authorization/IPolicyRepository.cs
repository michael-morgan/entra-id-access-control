using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository interface for managing Casbin policies.
/// Provides data access abstraction for policy CRUD operations.
/// </summary>
public interface IPolicyRepository
{
    /// <summary>
    /// Gets all policies for a specific workstream with optional filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID to filter by (includes global policies with null workstream)</param>
    /// <param name="policyType">Optional policy type filter (p, g, g2)</param>
    /// <param name="search">Optional search term for V0, V1, V2 fields</param>
    /// <returns>Collection of policies matching the criteria</returns>
    Task<IEnumerable<CasbinPolicy>> SearchAsync(
        string workstream,
        string? policyType = null,
        string? search = null);

    /// <summary>
    /// Gets a single policy by its ID.
    /// </summary>
    /// <param name="id">The policy ID</param>
    /// <returns>The policy if found, null otherwise</returns>
    Task<CasbinPolicy?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new policy.
    /// </summary>
    /// <param name="policy">The policy to create</param>
    /// <returns>The created policy with ID populated</returns>
    Task<CasbinPolicy> CreateAsync(CasbinPolicy policy);

    /// <summary>
    /// Updates an existing policy.
    /// </summary>
    /// <param name="policy">The policy to update</param>
    Task UpdateAsync(CasbinPolicy policy);

    /// <summary>
    /// Deletes a policy by ID.
    /// </summary>
    /// <param name="id">The policy ID to delete</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Checks if a policy exists by ID.
    /// </summary>
    /// <param name="id">The policy ID to check</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Gets all distinct policy types in the system.
    /// </summary>
    /// <param name="workstream">The workstream ID to filter by</param>
    /// <returns>Collection of policy type strings</returns>
    Task<IEnumerable<string>> GetPolicyTypesAsync(string workstream);

    /// <summary>
    /// Gets all distinct workstream IDs from policies.
    /// </summary>
    /// <returns>Collection of workstream IDs</returns>
    Task<IEnumerable<string>> GetWorkstreamsAsync();

    /// <summary>
    /// Gets policies by subject IDs (V0) and policy type across all workstreams.
    /// Efficient method for querying multiple subjects at once.
    /// </summary>
    /// <param name="subjectIds">Collection of subject IDs (user IDs, group IDs, role names)</param>
    /// <param name="policyType">Optional policy type filter (p, g, g2)</param>
    /// <returns>Collection of policies where V0 matches any of the subject IDs</returns>
    Task<IEnumerable<CasbinPolicy>> GetBySubjectIdsAsync(IEnumerable<string> subjectIds, string? policyType = null);

    /// <summary>
    /// Gets role mappings for multiple subjects in a specific workstream.
    /// Optimized batch query for role resolution.
    /// </summary>
    /// <param name="subjectIds">Collection of subject IDs (user IDs, group IDs)</param>
    /// <param name="workstream">The workstream ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of 'g' policies where V0 matches subject IDs and V2 matches workstream</returns>
    Task<IEnumerable<CasbinPolicy>> GetRolesForSubjectsAsync(
        IEnumerable<string> subjectIds,
        string workstream,
        CancellationToken cancellationToken = default);
}
