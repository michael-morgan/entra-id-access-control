using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Services.Graph;

/// <summary>
/// Database-backed group service that implements IGraphGroupService without calling Graph API.
/// Queries auth.Groups table for enriched group information.
/// Used when the GraphApi feature flag is disabled.
/// </summary>
public class DatabaseGroupService(
    IGroupRepository groupRepository,
    IUserGroupRepository userGroupRepository,
    ILogger<DatabaseGroupService> logger) : IGraphGroupService
{
    private readonly IGroupRepository _groupRepository = groupRepository;
    private readonly IUserGroupRepository _userGroupRepository = userGroupRepository;
    private readonly ILogger<DatabaseGroupService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<Group>> GetAllGroupsAsync()
    {
        _logger.LogDebug("Getting all groups from database");

        var groups = await _groupRepository.GetAllAsync();
        var graphGroups = groups.Select(MapToGraphGroup).ToList();

        _logger.LogInformation("Retrieved {Count} groups from database", graphGroups.Count);
        return graphGroups;
    }

    /// <inheritdoc />
    public async Task<Group?> GetGroupByIdAsync(string groupId)
    {
        _logger.LogDebug("Getting group {GroupId} from database", groupId);

        var group = await _groupRepository.GetByGroupIdAsync(groupId);
        if (group == null)
        {
            _logger.LogDebug("Group {GroupId} not found in database", groupId);
            return null;
        }

        return MapToGraphGroup(group);
    }

    /// <inheritdoc />
    public async Task<GroupWithMembers?> GetGroupWithMembersAsync(string groupId)
    {
        _logger.LogDebug("Getting group {GroupId} with members from database", groupId);

        var group = await _groupRepository.GetByGroupIdAsync(groupId);
        if (group == null)
        {
            _logger.LogDebug("Group {GroupId} not found in database", groupId);
            return null;
        }

        // Get members from UserGroups associations
        var userIds = await _userGroupRepository.GetUserIdsByGroupIdAsync(groupId);

        // Convert user IDs to DirectoryObject instances
        var members = userIds.Select(userId => new Microsoft.Graph.Models.DirectoryObject
        {
            Id = userId
        }).Cast<Microsoft.Graph.Models.DirectoryObject>().ToList();

        _logger.LogDebug("Group {GroupId} has {Count} members", groupId, members.Count);

        return new GroupWithMembers
        {
            Group = MapToGraphGroup(group),
            Members = members
        };
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Group>> GetGroupsByIdsAsync(List<string> groupIds)
    {
        _logger.LogDebug("Getting {Count} groups by IDs from database", groupIds.Count);

        var groups = await _groupRepository.GetByGroupIdsAsync(groupIds);
        var result = new Dictionary<string, Group>();

        foreach (var group in groups)
        {
            result[group.GroupId] = MapToGraphGroup(group);
        }

        _logger.LogInformation("Retrieved {Found}/{Total} groups from database", result.Count, groupIds.Count);
        return result;
    }

    /// <inheritdoc />
    public async Task<List<Group>> SearchGroupsAsync(string searchTerm)
    {
        _logger.LogDebug("Searching groups for '{SearchTerm}' in database", searchTerm);

        var groups = await _groupRepository.SearchAsync(searchTerm);
        var graphGroups = groups.Select(MapToGraphGroup).ToList();

        _logger.LogInformation("Found {Count} groups matching '{SearchTerm}'", graphGroups.Count, searchTerm);
        return graphGroups;
    }

    /// <summary>
    /// Maps a database Group entity to a Microsoft.Graph.Models.Group object.
    /// Uses DisplayName from database, falling back to GroupId (OID) if not enriched.
    /// </summary>
    private Group MapToGraphGroup(Api.Modules.AccessControl.Persistence.Entities.Authorization.Group dbGroup)
    {
        return new Group
        {
            Id = dbGroup.GroupId,
            DisplayName = dbGroup.DisplayName ?? dbGroup.GroupId, // Fallback to OID if not enriched
            Description = dbGroup.Description
        };
    }
}
