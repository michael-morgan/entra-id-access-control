using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository interface for managing Casbin roles.
/// Provides data access abstraction for role CRUD operations.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Gets all roles for a specific workstream with optional filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID to filter by (includes global roles with "*")</param>
    /// <param name="search">Optional search term for RoleName, DisplayName, or Description</param>
    /// <returns>Collection of roles matching the criteria</returns>
    Task<IEnumerable<CasbinRole>> SearchAsync(
        string workstream,
        string? search = null);

    /// <summary>
    /// Gets a single role by its ID.
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <returns>The role if found, null otherwise</returns>
    Task<CasbinRole?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a role by role name and workstream.
    /// </summary>
    /// <param name="roleName">The role name</param>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>The role if found, null otherwise</returns>
    Task<CasbinRole?> GetByRoleNameAsync(string roleName, string workstream);

    /// <summary>
    /// Checks if a role with the given name exists in the workstream (excluding a specific ID).
    /// </summary>
    /// <param name="roleName">The role name to check</param>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates)</param>
    /// <returns>True if a duplicate exists, false otherwise</returns>
    Task<bool> IsDuplicateRoleNameAsync(string roleName, string workstream, int? excludeId = null);

    /// <summary>
    /// Checks if a role is referenced in any policies.
    /// </summary>
    /// <param name="roleName">The role name to check</param>
    /// <returns>True if referenced, false otherwise</returns>
    Task<bool> IsReferencedInPoliciesAsync(string roleName);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="role">The role to create</param>
    /// <returns>The created role with ID populated</returns>
    Task<CasbinRole> CreateAsync(CasbinRole role);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <param name="role">The role to update</param>
    Task UpdateAsync(CasbinRole role);

    /// <summary>
    /// Deletes a role by ID.
    /// </summary>
    /// <param name="id">The role ID to delete</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Checks if a role exists by ID.
    /// </summary>
    /// <param name="id">The role ID to check</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(int id);
}
