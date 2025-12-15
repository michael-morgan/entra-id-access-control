using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Stores and retrieves role attributes for ABAC.
/// Attributes are scoped per workstream.
/// </summary>
public interface IRoleAttributeStore
{
    /// <summary>
    /// Gets role attributes by app role ID and workstream (with caching).
    /// </summary>
    Task<RoleAttributes?> GetAttributesByRoleIdAsync(
        string roleId,
        string workstreamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role attributes for multiple role IDs within a workstream (with caching).
    /// </summary>
    Task<IDictionary<string, RoleAttributes>> GetAttributesByRoleIdsAsync(
        IEnumerable<string> roleIds,
        string workstreamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates role attributes.
    /// </summary>
    Task UpdateAttributesAsync(
        RoleAttributes attributes,
        CancellationToken cancellationToken = default);
}
