using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository interface for managing users.
/// Provides data access abstraction for user CRUD operations.
/// Users are global (not workstream-scoped) since they exist across all workstreams.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their Entra ID user ID (oid claim).
    /// </summary>
    /// <param name="userId">The user ID from Entra ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all users</returns>
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by a collection of user IDs.
    /// </summary>
    /// <param name="userIds">Collection of user IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of users matching the provided IDs</returns>
    Task<IEnumerable<User>> GetByUserIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches users by name.
    /// </summary>
    /// <param name="searchTerm">Search term to match against user names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of users matching the search criteria</returns>
    Task<IEnumerable<User>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user</returns>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user by their user ID.
    /// </summary>
    /// <param name="userId">The user ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
}
