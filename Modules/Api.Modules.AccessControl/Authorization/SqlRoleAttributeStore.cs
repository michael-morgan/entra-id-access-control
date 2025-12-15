using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Stores and retrieves role attributes with caching.
/// </summary>
public class SqlRoleAttributeStore(
    AccessControlDbContext context,
    IMemoryCache cache,
    ILogger<SqlRoleAttributeStore> logger) : IRoleAttributeStore
{
    private readonly AccessControlDbContext _context = context;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<SqlRoleAttributeStore> _logger = logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public async Task<RoleAttributes?> GetAttributesByRoleIdAsync(
        string roleId,
        string workstreamId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"RoleAttributes:RoleId:{roleId}:{workstreamId}";

        if (_cache.TryGetValue<RoleAttributes>(cacheKey, out var cached))
        {
            return cached;
        }

        var entity = await _context.RoleAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleId == roleId && r.WorkstreamId == workstreamId && r.IsActive, cancellationToken);

        if (entity == null)
        {
            _logger.LogDebug("Role attributes not found for role ID {RoleId} in workstream {WorkstreamId}", roleId, workstreamId);
            return null;
        }

        var attributes = MapToModel(entity);
        _cache.Set(cacheKey, attributes, CacheDuration);

        return attributes;
    }

    public async Task<IDictionary<string, RoleAttributes>> GetAttributesByRoleIdsAsync(
        IEnumerable<string> roleIds,
        string workstreamId,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, RoleAttributes>();
        var uncachedIds = new List<string>();

        // Check cache first
        foreach (var roleId in roleIds)
        {
            var cacheKey = $"RoleAttributes:RoleId:{roleId}:{workstreamId}";
            if (_cache.TryGetValue<RoleAttributes>(cacheKey, out var cached) && cached != null)
            {
                result[roleId] = cached;
            }
            else
            {
                uncachedIds.Add(roleId);
            }
        }

        // Fetch uncached from database
        if (uncachedIds.Count > 0)
        {
            var entities = await _context.RoleAttributes
                .AsNoTracking()
                .Where(r => uncachedIds.Contains(r.RoleId) && r.WorkstreamId == workstreamId && r.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var entity in entities)
            {
                var attributes = MapToModel(entity);

                var roleIdKey = $"RoleAttributes:RoleId:{entity.RoleId}:{workstreamId}";

                _cache.Set(roleIdKey, attributes, CacheDuration);

                result[entity.RoleId] = attributes;
            }
        }

        return result;
    }

    public async Task UpdateAttributesAsync(
        RoleAttributes attributes,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.RoleAttributes
            .FirstOrDefaultAsync(r => r.RoleId == attributes.RoleId && r.WorkstreamId == attributes.WorkstreamId, cancellationToken);

        if (entity == null)
        {
            entity = new Persistence.Entities.Authorization.RoleAttribute
            {
                RoleId = attributes.RoleId,
                WorkstreamId = attributes.WorkstreamId,
                RoleName = attributes.RoleName,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.RoleAttributes.Add(entity);
        }

        entity.RoleName = attributes.RoleName;
        entity.AttributesJson = SerializeAttributes(attributes.Attributes);
        entity.ModifiedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var roleIdKey = $"RoleAttributes:RoleId:{attributes.RoleId}:{attributes.WorkstreamId}";
        _cache.Remove(roleIdKey);

        _logger.LogInformation(
            "Updated role attributes for {RoleId} ({RoleName}) in workstream {WorkstreamId}",
            attributes.RoleId, attributes.RoleName, attributes.WorkstreamId);
    }

    private static RoleAttributes MapToModel(Persistence.Entities.Authorization.RoleAttribute entity)
    {
        return new RoleAttributes
        {
            RoleId = entity.RoleId,
            WorkstreamId = entity.WorkstreamId,
            RoleName = entity.RoleName,
            Attributes = DeserializeAttributes(entity.AttributesJson)
        };
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
