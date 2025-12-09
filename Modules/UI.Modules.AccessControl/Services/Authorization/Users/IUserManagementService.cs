using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Authorization.Users;

/// <summary>
/// Service interface for managing users with business logic.
/// Orchestrates Graph API calls, repository calls, and ViewModel mapping.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Gets user details including groups, attributes, and role assignments.
    /// </summary>
    /// <param name="userId">The user ID (oid)</param>
    /// <returns>User details view model or null if not found</returns>
    Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId);

    /// <summary>
    /// Gets data for managing user roles.
    /// </summary>
    /// <param name="userId">The user ID (oid)</param>
    /// <returns>Manage roles view model or null if user not found</returns>
    Task<ManageRolesViewModel?> GetManageRolesDataAsync(string userId);
}
