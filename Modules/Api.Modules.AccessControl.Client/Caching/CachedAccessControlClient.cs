using System.Text.Json;
using Api.Modules.AccessControl.Client.Configuration;
using Api.Modules.AccessControl.Client.Http;
using Api.Modules.AccessControl.Client.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Client.Caching;

/// <summary>
/// Redis caching decorator for AccessControl client.
/// Implements cache-aside pattern to reduce API calls.
/// </summary>
public class CachedAccessControlClient : IAccessControlClient
{
    private readonly IAccessControlClient _innerClient;
    private readonly IDistributedCache _cache;
    private readonly AccessControlClientOptions _options;
    private readonly ILogger<CachedAccessControlClient> _logger;

    public CachedAccessControlClient(
        IAccessControlClient innerClient,
        IDistributedCache cache,
        IOptions<AccessControlClientOptions> options,
        ILogger<CachedAccessControlClient> logger)
    {
        _innerClient = innerClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthorizationCheckResponse> CheckAuthorizationAsync(
        string resource,
        string action,
        string? workstreamId = null,
        object? entityData = null,
        CancellationToken cancellationToken = default)
    {
        // Skip caching if entity data is provided (ABAC evaluation depends on dynamic data)
        if (entityData != null)
        {
            _logger.LogDebug("Skipping cache due to entity data (ABAC evaluation)");
            return await _innerClient.CheckAuthorizationAsync(resource, action, workstreamId, entityData, cancellationToken);
        }

        var cacheKey = BuildCacheKey("check", workstreamId ?? _options.DefaultWorkstreamId ?? "default", resource, action);

        // Try to get from cache
        var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedJson))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<AuthorizationCheckResponse>(cachedJson);
                if (cached != null)
                {
                    _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
                    return cached;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached authorization response");
            }
        }

        _logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);

        // Call API
        var response = await _innerClient.CheckAuthorizationAsync(resource, action, workstreamId, entityData, cancellationToken);

        // Store in cache
        try
        {
            var json = JsonSerializer.Serialize(response);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.CacheExpirationSeconds)
            };

            await _cache.SetStringAsync(cacheKey, json, cacheOptions, cancellationToken);
            _logger.LogDebug("Cached authorization response: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache authorization response");
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<List<AuthorizationCheckResponse>> CheckBatchAuthorizationAsync(
        string workstreamId,
        List<ResourceActionCheck> checks,
        CancellationToken cancellationToken = default)
    {
        // Batch checks are typically used for UI initialization, cache individually
        var results = new List<AuthorizationCheckResponse>();

        foreach (var check in checks)
        {
            var result = await CheckAuthorizationAsync(
                check.Resource,
                check.Action,
                workstreamId,
                entityData: null, // Batch checks don't include entity data
                cancellationToken
            );
            results.Add(result);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthorizedAsync(
        string resource,
        string action,
        string? workstreamId = null,
        object? entityData = null,
        CancellationToken cancellationToken = default)
    {
        var response = await CheckAuthorizationAsync(resource, action, workstreamId, entityData, cancellationToken);
        return response.Allowed;
    }

    /// <summary>
    /// Builds a cache key for authorization decisions.
    /// Format: accesscontrol:{type}:{workstream}:{resource}:{action}
    /// </summary>
    private string BuildCacheKey(string type, string workstreamId, string? resource = null, string? action = null)
    {
        var parts = new List<string> { "accesscontrol", type, workstreamId };

        if (!string.IsNullOrWhiteSpace(resource))
        {
            parts.Add(resource.Replace(":", "_")); // Escape colons
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            parts.Add(action);
        }

        return string.Join(":", parts);
    }
}
