using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Stores and retrieves group attributes with caching.
/// </summary>
public class SqlGroupAttributeStore(
    AccessControlDbContext context,
    IMemoryCache cache,
    ILogger<SqlGroupAttributeStore> logger) : IGroupAttributeStore
{
    private readonly AccessControlDbContext _context = context;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<SqlGroupAttributeStore> _logger = logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public async Task<GroupAttributes?> GetAttributesAsync(
        string groupId,
        string workstreamId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"GroupAttributes:{groupId}:{workstreamId}";

        if (_cache.TryGetValue<GroupAttributes>(cacheKey, out var cached))
        {
            return cached;
        }

        var entity = await _context.GroupAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GroupId == groupId && g.WorkstreamId == workstreamId && g.IsActive, cancellationToken);

        if (entity == null)
        {
            _logger.LogDebug("Group attributes not found for group {GroupId} in workstream {WorkstreamId}", groupId, workstreamId);
            return null;
        }

        var attributes = new GroupAttributes
        {
            GroupId = entity.GroupId,
            WorkstreamId = entity.WorkstreamId,
            GroupName = entity.GroupName,
            Attributes = DeserializeAttributes(entity.AttributesJson)
        };

        _cache.Set(cacheKey, attributes, CacheDuration);

        return attributes;
    }

    public async Task<IDictionary<string, GroupAttributes>> GetAttributesAsync(
        IEnumerable<string> groupIds,
        string workstreamId,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, GroupAttributes>();
        var uncachedIds = new List<string>();

        // Check cache first
        foreach (var groupId in groupIds)
        {
            var cacheKey = $"GroupAttributes:{groupId}:{workstreamId}";
            if (_cache.TryGetValue<GroupAttributes>(cacheKey, out var cached) && cached != null)
            {
                result[groupId] = cached;
            }
            else
            {
                uncachedIds.Add(groupId);
            }
        }

        // Fetch uncached from database
        if (uncachedIds.Count > 0)
        {
            var entities = await _context.GroupAttributes
                .AsNoTracking()
                .Where(g => uncachedIds.Contains(g.GroupId) && g.WorkstreamId == workstreamId && g.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var entity in entities)
            {
                var attributes = new GroupAttributes
                {
                    GroupId = entity.GroupId,
                    WorkstreamId = entity.WorkstreamId,
                    GroupName = entity.GroupName,
                    Attributes = DeserializeAttributes(entity.AttributesJson)
                };

                var cacheKey = $"GroupAttributes:{entity.GroupId}:{workstreamId}";
                _cache.Set(cacheKey, attributes, CacheDuration);
                result[entity.GroupId] = attributes;
            }
        }

        return result;
    }

    public async Task UpdateAttributesAsync(
        GroupAttributes attributes,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.GroupAttributes
            .FirstOrDefaultAsync(g => g.GroupId == attributes.GroupId && g.WorkstreamId == attributes.WorkstreamId, cancellationToken);

        if (entity == null)
        {
            entity = new Persistence.Entities.Authorization.GroupAttribute
            {
                GroupId = attributes.GroupId,
                WorkstreamId = attributes.WorkstreamId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.GroupAttributes.Add(entity);
        }

        entity.GroupName = attributes.GroupName;
        entity.AttributesJson = SerializeAttributes(attributes.Attributes);
        entity.ModifiedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"GroupAttributes:{attributes.GroupId}:{attributes.WorkstreamId}";
        _cache.Remove(cacheKey);

        _logger.LogInformation(
            "Updated group attributes for {GroupId} in workstream {WorkstreamId}", attributes.GroupId, attributes.WorkstreamId);
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
