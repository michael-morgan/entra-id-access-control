using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Attributes;

/// <summary>
/// Repository interface for managing role attributes.
/// Provides data access abstraction for role attribute CRUD operations.
/// </summary>
public interface IRoleAttributeRepository
{
    /// <summary>
    /// Gets all role attributes for a specific workstream with optional filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term for RoleValue or RoleDisplayName</param>
    /// <returns>Collection of role attributes matching the criteria</returns>
    Task<IEnumerable<RoleAttribute>> SearchAsync(string workstream, string? search = null);

    /// <summary>
    /// Gets a single role attribute by ID.
    /// </summary>
    /// <param name="id">The role attribute ID</param>
    /// <returns>The role attribute if found, null otherwise</returns>
    Task<RoleAttribute?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a role attribute by app role ID, role value, and workstream.
    /// </summary>
    /// <param name="appRoleId">The application role ID</param>
    /// <param name="roleValue">The role value</param>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>The role attribute if found, null otherwise</returns>
    Task<RoleAttribute?> GetByRoleAndWorkstreamAsync(string appRoleId, string roleValue, string workstream);

    /// <summary>
    /// Creates a new role attribute.
    /// </summary>
    /// <param name="roleAttribute">The role attribute to create</param>
    /// <returns>The created role attribute with ID populated</returns>
    Task<RoleAttribute> CreateAsync(RoleAttribute roleAttribute);

    /// <summary>
    /// Updates an existing role attribute.
    /// </summary>
    /// <param name="roleAttribute">The role attribute to update</param>
    Task UpdateAsync(RoleAttribute roleAttribute);

    /// <summary>
    /// Deletes a role attribute by ID.
    /// </summary>
    /// <param name="id">The role attribute ID to delete</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Checks if a role attribute exists.
    /// </summary>
    /// <param name="id">The role attribute ID</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(int id);
}
