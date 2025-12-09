using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Attributes;

/// <summary>
/// Repository interface for managing group attributes.
/// Provides data access abstraction for group attribute CRUD operations.
/// </summary>
public interface IGroupAttributeRepository
{
    /// <summary>
    /// Gets all group attributes for a specific workstream with optional filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term for GroupId or GroupName</param>
    /// <returns>Collection of group attributes matching the criteria</returns>
    Task<IEnumerable<GroupAttribute>> SearchAsync(string workstream, string? search = null);

    /// <summary>
    /// Gets a single group attribute by ID.
    /// </summary>
    /// <param name="id">The group attribute ID</param>
    /// <returns>The group attribute if found, null otherwise</returns>
    Task<GroupAttribute?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a group attribute by group ID and workstream.
    /// </summary>
    /// <param name="groupId">The group ID</param>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>The group attribute if found, null otherwise</returns>
    Task<GroupAttribute?> GetByGroupIdAndWorkstreamAsync(string groupId, string workstream);

    /// <summary>
    /// Creates a new group attribute.
    /// </summary>
    /// <param name="groupAttribute">The group attribute to create</param>
    /// <returns>The created group attribute with ID populated</returns>
    Task<GroupAttribute> CreateAsync(GroupAttribute groupAttribute);

    /// <summary>
    /// Updates an existing group attribute.
    /// </summary>
    /// <param name="groupAttribute">The group attribute to update</param>
    Task UpdateAsync(GroupAttribute groupAttribute);

    /// <summary>
    /// Updates multiple group attributes (for batch operations like display name sync).
    /// </summary>
    /// <param name="groupAttributes">The group attributes to update</param>
    Task UpdateManyAsync(IEnumerable<GroupAttribute> groupAttributes);

    /// <summary>
    /// Deletes a group attribute by ID.
    /// </summary>
    /// <param name="id">The group attribute ID to delete</param>
    Task DeleteAsync(int id);
}
