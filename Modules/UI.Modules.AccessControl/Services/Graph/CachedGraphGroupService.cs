using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Services.Graph;

/// <summary>
/// Cached wrapper for GraphGroupService to reduce Microsoft Graph API calls.
/// Uses in-memory caching with configurable expiration times.
/// </summary>
public class CachedGraphGroupService(
    GraphGroupService graphGroupService,
    IMemoryCache cache,
    ILogger<CachedGraphGroupService> logger)
{
    private readonly GraphGroupService _graphGroupService = graphGroupService;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<CachedGraphGroupService> _logger = logger;

    // Cache expiration times (following existing AttributeStore pattern)
    private static readonly TimeSpan GroupByIdExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AllGroupsExpiry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SearchResultsExpiry = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Get group by ID with caching
    /// </summary>
    public async Task<Group?> GetGroupByIdAsync(string groupId)
    {
        var cacheKey = $"Graph:Group:{groupId}";

        if (_cache.TryGetValue(cacheKey, out Group? cachedGroup))
        {
            _logger.LogDebug("Cache hit for group {GroupId}", groupId);
            return cachedGroup;
        }

        _logger.LogDebug("Cache miss for group {GroupId}, fetching from Graph API", groupId);
        var group = await _graphGroupService.GetGroupByIdAsync(groupId);

        if (group != null)
        {
            _cache.Set(cacheKey, group, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = GroupByIdExpiry
            });
        }

        return group;
    }

    /// <summary>
    /// Get multiple groups by IDs with caching (batch operation)
    /// </summary>
    public async Task<Dictionary<string, Group>> GetGroupsByIdsAsync(List<string> groupIds)
    {
        var result = new Dictionary<string, Group>();
        var uncachedIds = new List<string>();

        // Check cache for each group
        foreach (var groupId in groupIds)
        {
            var cacheKey = $"Graph:Group:{groupId}";
            if (_cache.TryGetValue(cacheKey, out Group? cachedGroup) && cachedGroup != null)
            {
                result[groupId] = cachedGroup;
                _logger.LogDebug("Cache hit for group {GroupId}", groupId);
            }
            else
            {
                uncachedIds.Add(groupId);
            }
        }

        // Fetch uncached groups from Graph API
        if (uncachedIds.Count > 0)
        {
            _logger.LogDebug("Cache miss for {Count} groups, fetching from Graph API", uncachedIds.Count);
            var fetchedGroups = await _graphGroupService.GetGroupsByIdsAsync(uncachedIds);

            // Add fetched groups to result and cache
            foreach (var (groupId, group) in fetchedGroups)
            {
                result[groupId] = group;
                var cacheKey = $"Graph:Group:{groupId}";
                _cache.Set(cacheKey, group, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = GroupByIdExpiry
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Get all groups with caching
    /// </summary>
    public async Task<List<Group>> GetAllGroupsAsync()
    {
        const string cacheKey = "Graph:Groups:All";

        if (_cache.TryGetValue(cacheKey, out List<Group>? cachedGroups) && cachedGroups != null)
        {
            _logger.LogDebug("Cache hit for all groups");
            return cachedGroups;
        }

        _logger.LogDebug("Cache miss for all groups, fetching from Graph API");
        var groups = await _graphGroupService.GetAllGroupsAsync();

        _cache.Set(cacheKey, groups, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AllGroupsExpiry
        });

        // Also cache individual groups
        foreach (var group in groups.Where(g => g.Id != null))
        {
            var groupCacheKey = $"Graph:Group:{group.Id}";
            _cache.Set(groupCacheKey, group, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = GroupByIdExpiry
            });
        }

        return groups;
    }

    /// <summary>
    /// Get group with members, caching the group with member data
    /// </summary>
    public async Task<GroupWithMembers?> GetGroupWithMembersAsync(string groupId)
    {
        var cacheKey = $"Graph:Group:{groupId}:WithMembers";

        if (_cache.TryGetValue(cacheKey, out GroupWithMembers? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for group with members {GroupId}", groupId);
            return cached;
        }

        _logger.LogDebug("Cache miss for group with members {GroupId}, fetching from Graph API", groupId);
        var groupWithMembers = await _graphGroupService.GetGroupWithMembersAsync(groupId);

        if (groupWithMembers != null)
        {
            _cache.Set(cacheKey, groupWithMembers, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = GroupByIdExpiry
            });
        }

        return groupWithMembers;
    }

    /// <summary>
    /// Search groups with short-lived caching (dynamic queries)
    /// </summary>
    public async Task<List<Group>> SearchGroupsAsync(string searchTerm)
    {
        var cacheKey = $"Graph:Groups:Search:{searchTerm}";

        if (_cache.TryGetValue(cacheKey, out List<Group>? cachedResults) && cachedResults != null)
        {
            _logger.LogDebug("Cache hit for group search '{SearchTerm}'", searchTerm);
            return cachedResults;
        }

        _logger.LogDebug("Cache miss for group search '{SearchTerm}', fetching from Graph API", searchTerm);
        var groups = await _graphGroupService.SearchGroupsAsync(searchTerm);

        _cache.Set(cacheKey, groups, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = SearchResultsExpiry
        });

        return groups;
    }
}
