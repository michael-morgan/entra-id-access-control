namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Caching layer for resolved roles from Casbin policies.
/// Prevents redundant role resolution queries for frequently accessed users/groups.
/// </summary>
public interface IRoleResolutionCache
{
    /// <summary>
    /// Gets cached resolved roles for a subject in a workstream.
    /// </summary>
    /// <param name="subject">Subject ID (user ID or group ID)</param>
    /// <param name="workstream">Workstream ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached roles if found, null otherwise</returns>
    Task<IEnumerable<string>?> GetAsync(string subject, string workstream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches resolved roles for a subject in a workstream.
    /// </summary>
    /// <param name="subject">Subject ID (user ID or group ID)</param>
    /// <param name="workstream">Workstream ID</param>
    /// <param name="roles">Resolved roles to cache</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync(string subject, string workstream, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached resolved roles for a subject in a workstream.
    /// </summary>
    /// <param name="subject">Subject ID (user ID or group ID)</param>
    /// <param name="workstream">Workstream ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string subject, string workstream, CancellationToken cancellationToken = default);
}
