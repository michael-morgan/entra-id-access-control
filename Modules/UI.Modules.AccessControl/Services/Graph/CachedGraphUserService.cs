using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Services.Graph;

/// <summary>
/// Cached wrapper for GraphUserService to reduce Microsoft Graph API calls.
/// Uses in-memory caching with configurable expiration times.
/// </summary>
public class CachedGraphUserService(
    GraphUserService graphUserService,
    IMemoryCache cache,
    ILogger<CachedGraphUserService> logger)
{
    private readonly GraphUserService _graphUserService = graphUserService;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<CachedGraphUserService> _logger = logger;

    // Cache expiration times (following existing AttributeStore pattern)
    private static readonly TimeSpan UserByIdExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AllUsersExpiry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan UserGroupsExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SearchResultsExpiry = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Get user by ID with caching
    /// </summary>
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        var cacheKey = $"Graph:User:{userId}";

        if (_cache.TryGetValue(cacheKey, out User? cachedUser))
        {
            _logger.LogDebug("Cache hit for user {UserId}", userId);
            return cachedUser;
        }

        _logger.LogDebug("Cache miss for user {UserId}, fetching from Graph API", userId);
        var user = await _graphUserService.GetUserByIdAsync(userId);

        if (user != null)
        {
            _cache.Set(cacheKey, user, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = UserByIdExpiry
            });
        }

        return user;
    }

    /// <summary>
    /// Get multiple users by IDs with caching (batch operation)
    /// </summary>
    public async Task<Dictionary<string, User>> GetUsersByIdsAsync(List<string> userIds)
    {
        var result = new Dictionary<string, User>();
        var uncachedIds = new List<string>();

        // Check cache for each user
        foreach (var userId in userIds)
        {
            var cacheKey = $"Graph:User:{userId}";
            if (_cache.TryGetValue(cacheKey, out User? cachedUser) && cachedUser != null)
            {
                result[userId] = cachedUser;
                _logger.LogDebug("Cache hit for user {UserId}", userId);
            }
            else
            {
                uncachedIds.Add(userId);
            }
        }

        // Fetch uncached users from Graph API
        if (uncachedIds.Count > 0)
        {
            _logger.LogDebug("Cache miss for {Count} users, fetching from Graph API", uncachedIds.Count);
            var fetchedUsers = await _graphUserService.GetUsersByIdsAsync(uncachedIds);

            // Add fetched users to result and cache
            foreach (var (userId, user) in fetchedUsers)
            {
                result[userId] = user;
                var cacheKey = $"Graph:User:{userId}";
                _cache.Set(cacheKey, user, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = UserByIdExpiry
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Get all users with caching
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        const string cacheKey = "Graph:Users:All";

        if (_cache.TryGetValue(cacheKey, out List<User>? cachedUsers) && cachedUsers != null)
        {
            _logger.LogDebug("Cache hit for all users");
            return cachedUsers;
        }

        _logger.LogDebug("Cache miss for all users, fetching from Graph API");
        var users = await _graphUserService.GetAllUsersAsync();

        _cache.Set(cacheKey, users, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AllUsersExpiry
        });

        // Also cache individual users
        foreach (var user in users.Where(u => u.Id != null))
        {
            var userCacheKey = $"Graph:User:{user.Id}";
            _cache.Set(userCacheKey, user, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = UserByIdExpiry
            });
        }

        return users;
    }

    /// <summary>
    /// Get user with groups, caching both user and groups
    /// </summary>
    public async Task<UserWithGroups?> GetUserWithGroupsAsync(string userId)
    {
        var cacheKey = $"Graph:User:{userId}:WithGroups";

        if (_cache.TryGetValue(cacheKey, out UserWithGroups? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for user with groups {UserId}", userId);
            return cached;
        }

        _logger.LogDebug("Cache miss for user with groups {UserId}, fetching from Graph API", userId);
        var userWithGroups = await _graphUserService.GetUserWithGroupsAsync(userId);

        if (userWithGroups != null)
        {
            _cache.Set(cacheKey, userWithGroups, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = UserGroupsExpiry
            });
        }

        return userWithGroups;
    }

    /// <summary>
    /// Get user's group IDs with caching
    /// </summary>
    public async Task<List<string>> GetUserGroupIdsAsync(string userId)
    {
        var cacheKey = $"Graph:User:{userId}:Groups";

        if (_cache.TryGetValue(cacheKey, out List<string>? cachedGroupIds) && cachedGroupIds != null)
        {
            _logger.LogDebug("Cache hit for user groups {UserId}", userId);
            return cachedGroupIds;
        }

        _logger.LogDebug("Cache miss for user groups {UserId}, fetching from Graph API", userId);
        var groupIds = await _graphUserService.GetUserGroupIdsAsync(userId);

        _cache.Set(cacheKey, groupIds, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = UserGroupsExpiry
        });

        return groupIds;
    }

    /// <summary>
    /// Search users with short-lived caching (dynamic queries)
    /// </summary>
    public async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        var cacheKey = $"Graph:Users:Search:{searchTerm}";

        if (_cache.TryGetValue(cacheKey, out List<User>? cachedResults) && cachedResults != null)
        {
            _logger.LogDebug("Cache hit for user search '{SearchTerm}'", searchTerm);
            return cachedResults;
        }

        _logger.LogDebug("Cache miss for user search '{SearchTerm}', fetching from Graph API", searchTerm);
        var users = await _graphUserService.SearchUsersAsync(searchTerm);

        _cache.Set(cacheKey, users, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = SearchResultsExpiry
        });

        return users;
    }
}
