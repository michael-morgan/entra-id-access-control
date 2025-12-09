using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Attributes;

/// <summary>
/// Repository interface for managing user attributes.
/// Provides data access abstraction for user attribute CRUD operations.
/// </summary>
public interface IUserAttributeRepository
{
    /// <summary>
    /// Gets all user attributes for a specific workstream with optional filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term for UserId</param>
    /// <returns>Collection of user attributes matching the criteria</returns>
    Task<IEnumerable<UserAttribute>> SearchAsync(string workstream, string? search = null);

    /// <summary>
    /// Gets a single user attribute by ID.
    /// </summary>
    /// <param name="id">The user attribute ID</param>
    /// <returns>The user attribute if found, null otherwise</returns>
    Task<UserAttribute?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a user attribute by user ID and workstream.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>The user attribute if found, null otherwise</returns>
    Task<UserAttribute?> GetByUserIdAndWorkstreamAsync(string userId, string workstream);

    /// <summary>
    /// Creates a new user attribute.
    /// </summary>
    /// <param name="userAttribute">The user attribute to create</param>
    /// <returns>The created user attribute with ID populated</returns>
    Task<UserAttribute> CreateAsync(UserAttribute userAttribute);

    /// <summary>
    /// Updates an existing user attribute.
    /// </summary>
    /// <param name="userAttribute">The user attribute to update</param>
    Task UpdateAsync(UserAttribute userAttribute);

    /// <summary>
    /// Deletes a user attribute by ID.
    /// </summary>
    /// <param name="id">The user attribute ID to delete</param>
    Task DeleteAsync(int id);
}
