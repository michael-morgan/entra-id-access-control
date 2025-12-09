using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Stores and retrieves user attributes with caching.
/// </summary>
public class SqlUserAttributeStore(
    AccessControlDbContext context,
    IMemoryCache cache,
    ILogger<SqlUserAttributeStore> logger) : IUserAttributeStore
{
    private readonly AccessControlDbContext _context = context;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<SqlUserAttributeStore> _logger = logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public async Task<UserAttributes?> GetAttributesAsync(
        string userId,
        string workstreamId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"UserAttributes:{userId}:{workstreamId}";

        if (_cache.TryGetValue<UserAttributes>(cacheKey, out var cached))
        {
            return cached;
        }

        var entity = await _context.UserAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.WorkstreamId == workstreamId, cancellationToken);

        if (entity == null)
        {
            _logger.LogDebug("User attributes not found for user {UserId} in workstream {WorkstreamId}", userId, workstreamId);
            return null;
        }

        var attributes = new UserAttributes
        {
            UserId = entity.UserId,
            WorkstreamId = entity.WorkstreamId,
            Attributes = DeserializeAttributes(entity.AttributesJson)
        };

        _cache.Set(cacheKey, attributes, CacheDuration);

        return attributes;
    }

    public async Task UpdateAttributesAsync(
        UserAttributes attributes,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.UserAttributes
            .FirstOrDefaultAsync(u => u.UserId == attributes.UserId && u.WorkstreamId == attributes.WorkstreamId, cancellationToken);

        if (entity == null)
        {
            entity = new UserAttribute
            {
                UserId = attributes.UserId,
                WorkstreamId = attributes.WorkstreamId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.UserAttributes.Add(entity);
        }

        entity.AttributesJson = SerializeAttributes(attributes.Attributes);
        entity.ModifiedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"UserAttributes:{attributes.UserId}:{attributes.WorkstreamId}";
        _cache.Remove(cacheKey);

        _logger.LogInformation(
            "Updated user attributes for {UserId} in workstream {WorkstreamId}", attributes.UserId, attributes.WorkstreamId);
    }

    private static Dictionary<string, JsonElement> DeserializeAttributes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? SerializeAttributes(Dictionary<string, JsonElement>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return null;

        return JsonSerializer.Serialize(attributes);
    }
}
