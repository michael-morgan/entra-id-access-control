using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Stores and retrieves user attributes for ABAC.
/// Attributes are scoped per workstream.
/// </summary>
public interface IUserAttributeStore
{
    /// <summary>
    /// Gets user attributes by user ID and workstream (with caching).
    /// </summary>
    Task<UserAttributes?> GetAttributesAsync(
        string userId,
        string workstreamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user attributes.
    /// </summary>
    Task UpdateAttributesAsync(
        UserAttributes attributes,
        CancellationToken cancellationToken = default);
}
