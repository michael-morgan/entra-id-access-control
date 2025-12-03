using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Stores and retrieves group attributes for ABAC.
/// Attributes are scoped per workstream.
/// </summary>
public interface IGroupAttributeStore
{
    /// <summary>
    /// Gets group attributes by group ID and workstream (with caching).
    /// </summary>
    Task<GroupAttributes?> GetAttributesAsync(
        string groupId,
        string workstreamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets group attributes for multiple group IDs within a workstream (with caching).
    /// </summary>
    Task<IDictionary<string, GroupAttributes>> GetAttributesAsync(
        IEnumerable<string> groupIds,
        string workstreamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates group attributes.
    /// </summary>
    Task UpdateAttributesAsync(
        GroupAttributes attributes,
        CancellationToken cancellationToken = default);
}
