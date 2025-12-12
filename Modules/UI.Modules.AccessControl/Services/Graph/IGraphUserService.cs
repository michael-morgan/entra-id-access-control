using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Services.Graph;

/// <summary>
/// Interface for user-related operations from Microsoft Graph API or alternative data sources.
/// </summary>
public interface IGraphUserService
{
    /// <summary>
    /// Get all users with basic properties.
    /// </summary>
    Task<List<User>> GetAllUsersAsync();

    /// <summary>
    /// Get a user by their ID.
    /// </summary>
    Task<User?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Get a user with their group memberships.
    /// </summary>
    Task<UserWithGroups?> GetUserWithGroupsAsync(string userId);

    /// <summary>
    /// Get the group IDs for a user.
    /// </summary>
    Task<List<string>> GetUserGroupIdsAsync(string userId);

    /// <summary>
    /// Search for users by a search term.
    /// </summary>
    Task<List<User>> SearchUsersAsync(string searchTerm);

    /// <summary>
    /// Get multiple users by their IDs.
    /// </summary>
    Task<Dictionary<string, User>> GetUsersByIdsAsync(List<string> userIds);
}
