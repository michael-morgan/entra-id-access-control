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
    Task<RoleAttributes?> GetAttributesByAppRoleIdAsync(
        string appRoleId,
        string workstreamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role attributes by role value and workstream (with caching).
    /// </summary>
    Task<RoleAttributes?> GetAttributesByRoleValueAsync(
        string roleValue,
        string workstreamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role attributes for multiple role values within a workstream (with caching).
    /// </summary>
    Task<IDictionary<string, RoleAttributes>> GetAttributesByRoleValuesAsync(
        IEnumerable<string> roleValues,
        string workstreamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates role attributes.
    /// </summary>
    Task UpdateAttributesAsync(
        RoleAttributes attributes,
        CancellationToken cancellationToken = default);
}
