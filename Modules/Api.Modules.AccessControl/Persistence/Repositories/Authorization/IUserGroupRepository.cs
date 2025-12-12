using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository interface for UserGroup entity operations.
/// Manages the many-to-many relationship between Users and Groups.
/// </summary>
public interface IUserGroupRepository
{
    /// <summary>
    /// Get all groups for a specific user with group details.
    /// </summary>
    Task<List<UserGroup>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all group IDs for a specific user (lightweight query without navigation properties).
    /// </summary>
    Task<List<string>> GetGroupIdsByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all user IDs for a specific group (lightweight query without navigation properties).
    /// </summary>
    Task<List<string>> GetUserIdsByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users in a specific group.
    /// </summary>
    Task<List<UserGroup>> GetGroupUsersAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert (insert or update) a user-group association.
    /// If association exists, updates LastSeenAt.
    /// If association doesn't exist, creates it.
    /// </summary>
    Task<UserGroup> UpsertUserGroupAsync(string userId, string groupId, string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stale user-group associations (LastSeenAt older than threshold).
    /// </summary>
    /// <param name="daysThreshold">Number of days to consider stale</param>
    /// <param name="cancellationToken"></param>
    /// <returns>List of stale associations with user and group details</returns>
    Task<List<UserGroup>> GetStaleAssociationsAsync(int daysThreshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user-group association by ID.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user-group association by ID with navigation properties.
    /// </summary>
    Task<UserGroup?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
