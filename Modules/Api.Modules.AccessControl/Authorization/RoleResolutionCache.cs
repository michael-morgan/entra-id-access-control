using Api.Modules.AccessControl.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// In-memory cache for resolved roles from Casbin policies.
/// Reduces redundant database queries for role resolution.
/// </summary>
public class RoleResolutionCache(IMemoryCache memoryCache) : IRoleResolutionCache
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public Task<IEnumerable<string>?> GetAsync(string subject, string workstream, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(subject, workstream);

        if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<string>? cachedRoles))
        {
            return Task.FromResult(cachedRoles);
        }

        return Task.FromResult<IEnumerable<string>?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(string subject, string workstream, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(subject, workstream);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheDuration,
            Size = 1 // For cache size limits if configured
        };

        // Store as array to prevent multiple enumeration
        _memoryCache.Set(cacheKey, roles.ToArray(), cacheOptions);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string subject, string workstream, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(subject, workstream);
        _memoryCache.Remove(cacheKey);

        return Task.CompletedTask;
    }

    private static string GetCacheKey(string subject, string workstream)
    {
        return $"RoleResolution:{subject}:{workstream}";
    }
}
