using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Services.Graph;

/// <summary>
/// Interface for group-related operations from Microsoft Graph API or alternative data sources.
/// </summary>
public interface IGraphGroupService
{
    /// <summary>
    /// Get all groups with basic properties.
    /// </summary>
    Task<List<Group>> GetAllGroupsAsync();

    /// <summary>
    /// Get a group by its ID.
    /// </summary>
    Task<Group?> GetGroupByIdAsync(string groupId);

    /// <summary>
    /// Get a group with its members.
    /// </summary>
    Task<GroupWithMembers?> GetGroupWithMembersAsync(string groupId);

    /// <summary>
    /// Get multiple groups by their IDs.
    /// </summary>
    Task<Dictionary<string, Group>> GetGroupsByIdsAsync(List<string> groupIds);

    /// <summary>
    /// Search for groups by a search term.
    /// </summary>
    Task<List<Group>> SearchGroupsAsync(string searchTerm);
}
