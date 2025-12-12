using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Microsoft.Graph.Models;
using UI.Modules.AccessControl.Services.Attributes;

namespace UI.Modules.AccessControl.Services.Graph;

/// <summary>
/// Database-backed user service that implements IGraphUserService without calling Graph API.
/// Used when the GraphApi feature flag is disabled.
/// </summary>
public class DatabaseUserService(
    IUserRepository userRepository,
    IGroupRepository groupRepository,
    IUserGroupRepository userGroupRepository,
    IGlobalAttributeService globalAttributeService,
    ILogger<DatabaseUserService> logger) : IGraphUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IGroupRepository _groupRepository = groupRepository;
    private readonly IUserGroupRepository _userGroupRepository = userGroupRepository;
    private readonly IGlobalAttributeService _globalAttributeService = globalAttributeService;
    private readonly ILogger<DatabaseUserService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<User>> GetAllUsersAsync()
    {
        _logger.LogDebug("Getting all users from database");

        var users = await _userRepository.GetAllAsync();
        var graphUsers = new List<User>();

        foreach (var user in users)
        {
            var graphUser = await MapToGraphUser(user.UserId, user.Name);
            graphUsers.Add(graphUser);
        }

        return graphUsers;
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        _logger.LogDebug("Getting user {UserId} from database", userId);

        var user = await _userRepository.GetByUserIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        return await MapToGraphUser(user.UserId, user.Name);
    }

    /// <inheritdoc />
    public async Task<UserWithGroups?> GetUserWithGroupsAsync(string userId)
    {
        _logger.LogDebug("Getting user {UserId} with groups from database", userId);

        var graphUser = await GetUserByIdAsync(userId);
        if (graphUser == null)
        {
            return null;
        }

        // Get groups from UserGroups associations
        var userGroupIds = await _userGroupRepository.GetGroupIdsByUserIdAsync(userId);
        var groups = new List<Microsoft.Graph.Models.Group>();

        foreach (var groupId in userGroupIds)
        {
            var group = await _groupRepository.GetByGroupIdAsync(groupId);
            if (group != null)
            {
                groups.Add(new Microsoft.Graph.Models.Group
                {
                    Id = group.GroupId,
                    DisplayName = group.DisplayName ?? group.GroupId, // Fallback to OID if not enriched
                    Description = group.Description
                });
            }
        }

        return new UserWithGroups
        {
            User = graphUser,
            Groups = groups
        };
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUserGroupIdsAsync(string userId)
    {
        _logger.LogDebug("Getting group IDs for user {UserId} from database", userId);

        // Get groups from UserGroups associations
        var groupIds = await _userGroupRepository.GetGroupIdsByUserIdAsync(userId);
        return groupIds;
    }

    /// <inheritdoc />
    public async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        _logger.LogDebug("Searching users for '{SearchTerm}' in database", searchTerm);

        var users = await _userRepository.SearchByNameAsync(searchTerm);
        var graphUsers = new List<User>();

        foreach (var user in users)
        {
            var graphUser = await MapToGraphUser(user.UserId, user.Name);
            graphUsers.Add(graphUser);
        }

        return graphUsers;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, User>> GetUsersByIdsAsync(List<string> userIds)
    {
        _logger.LogDebug("Getting {Count} users by IDs from database", userIds.Count);

        var users = await _userRepository.GetByUserIdsAsync(userIds);
        var result = new Dictionary<string, User>();

        foreach (var user in users)
        {
            var graphUser = await MapToGraphUser(user.UserId, user.Name);
            result[user.UserId] = graphUser;
        }

        return result;
    }

    /// <summary>
    /// Maps a database user to a Microsoft.Graph.Models.User object with attributes.
    /// </summary>
    private async Task<User> MapToGraphUser(string userId, string displayName)
    {
        // Get global attributes (JobTitle and Department)
        var jobTitle = await _globalAttributeService.GetUserJobTitleAsync(userId);
        var department = await _globalAttributeService.GetUserDepartmentAsync(userId);

        return new User
        {
            Id = userId,
            DisplayName = displayName,
            UserPrincipalName = null, // Not available from database
            Mail = null, // Not available from database
            JobTitle = jobTitle,
            Department = department
        };
    }
}
