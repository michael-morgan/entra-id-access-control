using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Services;

public class GraphUserService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphUserService> _logger;
    private readonly int _defaultPageSize;

    public GraphUserService(
        GraphServiceClient graphServiceClient,
        IConfiguration configuration,
        ILogger<GraphUserService> logger)
    {
        _graphClient = graphServiceClient;
        _logger = logger;
        _defaultPageSize = configuration.GetValue<int>("GraphApi:DefaultPageSize", 100);
    }

    /// <summary>
    /// Get all users with basic properties
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            var users = new List<User>();

            var response = await _graphClient.Users
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "userPrincipalName",
                        "mail",
                        "jobTitle",
                        "department"
                    };
                    requestConfig.QueryParameters.Top = _defaultPageSize;
                    requestConfig.QueryParameters.Orderby = new[] { "displayName" };
                });

            if (response?.Value == null)
            {
                return users;
            }

            // Handle pagination
            var pageIterator = PageIterator<User, UserCollectionResponse>
                .CreatePageIterator(
                    _graphClient,
                    response,
                    (user) =>
                    {
                        users.Add(user);
                        return true; // Continue iterating
                    });

            await pageIterator.IterateAsync();

            _logger.LogInformation("Retrieved {Count} users from Graph API", users.Count);
            return users;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error retrieving users: {StatusCode} - {Message}",
                ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _graphClient.Users[userId]
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "userPrincipalName",
                        "mail",
                        "jobTitle",
                        "department"
                    };
                });

            return user;
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogWarning("User {UserId} not found in Entra ID", userId);
            return null;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error retrieving user {UserId}: {StatusCode} - {Message}",
                userId, ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get user with their group memberships
    /// </summary>
    public async Task<UserWithGroups?> GetUserWithGroupsAsync(string userId)
    {
        try
        {
            // Get user details
            var user = await _graphClient.Users[userId]
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "userPrincipalName",
                        "mail",
                        "jobTitle",
                        "department"
                    };
                });

            if (user == null)
            {
                return null;
            }

            // Get user's group memberships
            var groups = new List<Group>();
            var memberOfResponse = await _graphClient.Users[userId]
                .MemberOf
                .GraphGroup // Filter to only groups (not directory roles)
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[] { "id", "displayName", "description" };
                    requestConfig.QueryParameters.Top = _defaultPageSize;
                });

            if (memberOfResponse?.Value != null)
            {
                // Handle pagination for groups
                var pageIterator = PageIterator<Group, GroupCollectionResponse>
                    .CreatePageIterator(
                        _graphClient,
                        memberOfResponse,
                        (group) =>
                        {
                            groups.Add(group);
                            return true;
                        });

                await pageIterator.IterateAsync();
            }

            return new UserWithGroups
            {
                User = user,
                Groups = groups
            };
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogWarning("User {UserId} not found in Entra ID", userId);
            return null;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error retrieving user {UserId} with groups: {StatusCode} - {Message}",
                userId, ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get group IDs for a user (optimized for authorization checks)
    /// </summary>
    public async Task<List<string>> GetUserGroupIdsAsync(string userId)
    {
        try
        {
            // Use getMemberGroups for efficient group ID retrieval
            // This returns all groups (direct and transitive)
            var request = new Microsoft.Graph.Users.Item.GetMemberGroups.GetMemberGroupsPostRequestBody
            {
                SecurityEnabledOnly = false // Include all groups, not just security groups
            };

            var response = await _graphClient.Users[userId]
                .GetMemberGroups
                .PostAsGetMemberGroupsPostResponseAsync(request);

            var groupIds = response?.Value;

            return groupIds?.ToList() ?? new List<string>();
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error fetching group IDs for user {UserId}: {StatusCode} - {Message}",
                userId, ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Search users by display name or email
    /// </summary>
    public async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        try
        {
            var users = new List<User>();

            var response = await _graphClient.Users
                .GetAsync(requestConfig =>
                {
                    // Use $search for fuzzy matching
                    requestConfig.QueryParameters.Search = $"\"displayName:{searchTerm}\" OR \"mail:{searchTerm}\"";
                    requestConfig.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "userPrincipalName",
                        "mail",
                        "jobTitle",
                        "department"
                    };
                    requestConfig.QueryParameters.Top = _defaultPageSize;

                    // $search requires ConsistencyLevel header
                    requestConfig.Headers.Add("ConsistencyLevel", "eventual");
                });

            if (response?.Value == null)
            {
                return users;
            }

            var pageIterator = PageIterator<User, UserCollectionResponse>
                .CreatePageIterator(_graphClient, response, user =>
                {
                    users.Add(user);
                    return true;
                });

            await pageIterator.IterateAsync();

            _logger.LogInformation("Found {Count} users matching search term '{SearchTerm}'",
                users.Count, searchTerm);

            return users;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error searching users: {Code} - {Message}",
                ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get multiple users by IDs using batch requests
    /// </summary>
    public async Task<Dictionary<string, User>> GetUsersByIdsAsync(List<string> userIds)
    {
        var result = new Dictionary<string, User>();

        if (!userIds.Any())
        {
            return result;
        }

        try
        {
            // Process in batches to avoid overwhelming the API
            var batchSize = 20; // Max batch size for Graph API
            var batches = userIds
                .Select((id, index) => new { id, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.id).ToList());

            foreach (var batch in batches)
            {
                var tasks = batch.Select(userId =>
                    _graphClient.Users[userId].GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select = new[]
                        {
                            "id",
                            "displayName",
                            "userPrincipalName",
                            "mail"
                        };
                    }));

                var users = await Task.WhenAll(tasks);

                foreach (var user in users.Where(u => u != null))
                {
                    if (user?.Id != null)
                    {
                        result[user.Id] = user;
                    }
                }
            }

            _logger.LogInformation("Retrieved {Count} out of {Total} requested users",
                result.Count, userIds.Count);

            return result;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error retrieving multiple users: {Code} - {Message}",
                ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }
}

public class UserWithGroups
{
    public User User { get; set; } = null!;
    public List<Group> Groups { get; set; } = new();
}
