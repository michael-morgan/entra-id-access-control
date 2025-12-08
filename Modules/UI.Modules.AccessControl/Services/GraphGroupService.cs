using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Services;

public class GraphGroupService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphGroupService> _logger;
    private readonly int _defaultPageSize;

    public GraphGroupService(
        GraphServiceClient graphServiceClient,
        IConfiguration configuration,
        ILogger<GraphGroupService> logger)
    {
        _graphClient = graphServiceClient;
        _logger = logger;
        _defaultPageSize = configuration.GetValue<int>("GraphApi:DefaultPageSize", 100);
    }

    /// <summary>
    /// Get all groups with basic properties
    /// </summary>
    public async Task<List<Group>> GetAllGroupsAsync()
    {
        try
        {
            var groups = new List<Group>();

            var response = await _graphClient.Groups
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "description",
                        "mailNickname"
                    };
                    requestConfig.QueryParameters.Top = _defaultPageSize;
                    requestConfig.QueryParameters.Orderby = new[] { "displayName" };
                });

            if (response?.Value == null)
            {
                return groups;
            }

            // Handle pagination
            var pageIterator = PageIterator<Group, GroupCollectionResponse>
                .CreatePageIterator(
                    _graphClient,
                    response,
                    (group) =>
                    {
                        groups.Add(group);
                        return true; // Continue iterating
                    });

            await pageIterator.IterateAsync();

            _logger.LogInformation("Retrieved {Count} groups from Graph API", groups.Count);
            return groups;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error retrieving groups: {Code} - {Message}",
                ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get a specific group by ID
    /// </summary>
    public async Task<Group?> GetGroupByIdAsync(string groupId)
    {
        try
        {
            var group = await _graphClient.Groups[groupId]
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "description",
                        "mailNickname"
                    };
                });

            return group;
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogWarning("Group {GroupId} not found in Entra ID", groupId);
            return null;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error retrieving group {GroupId}: {StatusCode} - {Message}",
                groupId, ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get group with its members
    /// </summary>
    public async Task<GroupWithMembers?> GetGroupWithMembersAsync(string groupId)
    {
        try
        {
            // Get group details
            var group = await _graphClient.Groups[groupId]
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "description",
                        "mailNickname"
                    };
                });

            if (group == null)
            {
                return null;
            }

            // Get group members
            var members = new List<DirectoryObject>();
            var membersResponse = await _graphClient.Groups[groupId]
                .Members
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Top = _defaultPageSize;
                });

            if (membersResponse?.Value != null)
            {
                // Handle pagination
                var pageIterator = PageIterator<DirectoryObject, DirectoryObjectCollectionResponse>
                    .CreatePageIterator(
                        _graphClient,
                        membersResponse,
                        (member) =>
                        {
                            members.Add(member);
                            return true;
                        });

                await pageIterator.IterateAsync();
            }

            return new GroupWithMembers
            {
                Group = group,
                Members = members
            };
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogWarning("Group {GroupId} not found in Entra ID", groupId);
            return null;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error retrieving group {GroupId} with members: {StatusCode} - {Message}",
                groupId, ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get multiple groups by IDs
    /// </summary>
    public async Task<Dictionary<string, Group>> GetGroupsByIdsAsync(List<string> groupIds)
    {
        var result = new Dictionary<string, Group>();

        if (!groupIds.Any())
        {
            return result;
        }

        try
        {
            // Process in batches to avoid overwhelming the API
            var batchSize = 20;
            var batches = groupIds
                .Select((id, index) => new { id, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.id).ToList());

            foreach (var batch in batches)
            {
                var tasks = batch.Select(groupId =>
                    _graphClient.Groups[groupId].GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select = new[]
                        {
                            "id",
                            "displayName",
                            "description"
                        };
                    }));

                var groups = await Task.WhenAll(tasks);

                foreach (var group in groups.Where(g => g != null))
                {
                    if (group?.Id != null)
                    {
                        result[group.Id] = group;
                    }
                }
            }

            _logger.LogInformation("Retrieved {Count} out of {Total} requested groups",
                result.Count, groupIds.Count);

            return result;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error retrieving multiple groups: {Code} - {Message}",
                ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Search groups by display name
    /// </summary>
    public async Task<List<Group>> SearchGroupsAsync(string searchTerm)
    {
        try
        {
            var groups = new List<Group>();

            var response = await _graphClient.Groups
                .GetAsync(requestConfig =>
                {
                    // Use $filter for searching (more reliable than $search for groups)
                    requestConfig.QueryParameters.Filter = $"startswith(displayName,'{searchTerm}')";
                    requestConfig.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "description",
                        "mailNickname"
                    };
                    requestConfig.QueryParameters.Top = _defaultPageSize;
                });

            if (response?.Value == null)
            {
                return groups;
            }

            var pageIterator = PageIterator<Group, GroupCollectionResponse>
                .CreatePageIterator(_graphClient, response, group =>
                {
                    groups.Add(group);
                    return true;
                });

            await pageIterator.IterateAsync();

            _logger.LogInformation("Found {Count} groups matching search term '{SearchTerm}'",
                groups.Count, searchTerm);

            return groups;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error searching groups: {Code} - {Message}",
                ex.ResponseStatusCode, ex.Message);
            throw;
        }
    }
}

public class GroupWithMembers
{
    public Group Group { get; set; } = null!;
    public List<DirectoryObject> Members { get; set; } = new();
}
