using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository interface for Group entity operations.
/// </summary>
public interface IGroupRepository
{
    /// <summary>
    /// Get a group by its ID (Entra OID).
    /// </summary>
    Task<Group?> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all groups.
    /// </summary>
    Task<IEnumerable<Group>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple groups by their IDs (batch lookup).
    /// </summary>
    Task<List<Group>> GetByGroupIdsAsync(List<string> groupIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new group.
    /// </summary>
    Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing group.
    /// </summary>
    Task UpdateAsync(Group group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search groups by display name or group ID.
    /// </summary>
    Task<IEnumerable<Group>> SearchAsync(string? search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create a group (upsert pattern).
    /// If group doesn't exist, creates it with provided details.
    /// If group exists, returns existing record without modification.
    /// </summary>
    /// <param name="groupId">Entra group OID</param>
    /// <param name="displayName">Optional display name (defaults to groupId if null)</param>
    /// <param name="source">Source of the group (JWT, Manual, GraphAPI)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Existing or newly created group</returns>
    Task<Group> GetOrCreateAsync(string groupId, string? displayName, string source, CancellationToken cancellationToken = default);
}
