using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Authorization.Roles;

/// <summary>
/// Service interface for managing Casbin roles with business logic.
/// Orchestrates repository calls and ViewModel mapping.
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Gets roles for a workstream with optional search filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term</param>
    /// <returns>Collection of roles</returns>
    Task<IEnumerable<CasbinRole>> GetRolesAsync(string workstream, string? search = null);

    /// <summary>
    /// Gets a single role by ID.
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <returns>The role if found, null otherwise</returns>
    Task<CasbinRole?> GetRoleByIdAsync(int id);

    /// <summary>
    /// Creates a new role with validation.
    /// </summary>
    /// <param name="model">The role view model</param>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="createdBy">The user creating the role</param>
    /// <returns>Tuple of (success, role, errorMessage)</returns>
    Task<(bool Success, CasbinRole? Role, string? ErrorMessage)> CreateRoleAsync(
        RoleViewModel model, string workstream, string createdBy);

    /// <summary>
    /// Updates an existing role with validation.
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="model">The updated role view model</param>
    /// <param name="modifiedBy">The user modifying the role</param>
    /// <returns>Tuple of (success, errorMessage)</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateRoleAsync(
        int id, RoleViewModel model, string modifiedBy);

    /// <summary>
    /// Deletes a role with validation (checks if system role or referenced in policies).
    /// </summary>
    /// <param name="id">The role ID to delete</param>
    /// <returns>Tuple of (success, errorMessage)</returns>
    Task<(bool Success, string? ErrorMessage)> DeleteRoleAsync(int id);

    /// <summary>
    /// Checks if a role exists.
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> RoleExistsAsync(int id);
}
