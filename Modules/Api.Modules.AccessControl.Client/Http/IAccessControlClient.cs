using Api.Modules.AccessControl.Client.Models;

namespace Api.Modules.AccessControl.Client.Http;

/// <summary>
/// Client interface for calling the AccessControl authorization API.
/// </summary>
public interface IAccessControlClient
{
    /// <summary>
    /// Check if the authenticated user is authorized to perform an action on a resource.
    /// </summary>
    /// <param name="resource">The resource being accessed (e.g., "Loan/123")</param>
    /// <param name="action">The action being performed (e.g., "approve")</param>
    /// <param name="workstreamId">Optional workstream ID (uses default if not provided)</param>
    /// <param name="entityData">Optional entity data for ABAC evaluation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization decision</returns>
    Task<AuthorizationCheckResponse> CheckAuthorizationAsync(
        string resource,
        string action,
        string? workstreamId = null,
        object? entityData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check multiple resource/action combinations in a single batch request.
    /// More efficient than multiple single checks when loading a page with many authorization decisions.
    /// </summary>
    /// <param name="workstreamId">The workstream context for all checks</param>
    /// <param name="checks">List of resource/action combinations to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of authorization decisions</returns>
    Task<List<AuthorizationCheckResponse>> CheckBatchAuthorizationAsync(
        string workstreamId,
        List<ResourceActionCheck> checks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience method: Check if user is authorized (returns only boolean).
    /// </summary>
    /// <param name="resource">The resource being accessed</param>
    /// <param name="action">The action being performed</param>
    /// <param name="workstreamId">Optional workstream ID</param>
    /// <param name="entityData">Optional entity data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if authorized, false otherwise</returns>
    Task<bool> IsAuthorizedAsync(
        string resource,
        string action,
        string? workstreamId = null,
        object? entityData = null,
        CancellationToken cancellationToken = default);
}
