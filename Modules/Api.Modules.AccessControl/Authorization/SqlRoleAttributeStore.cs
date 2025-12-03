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
public class SqlRoleAttributeStore : IRoleAttributeStore
{
    private readonly AccessControlDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SqlRoleAttributeStore> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public SqlRoleAttributeStore(
        AccessControlDbContext context,
        IMemoryCache cache,
        ILogger<SqlRoleAttributeStore> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RoleAttributes?> GetAttributesByAppRoleIdAsync(
        string appRoleId,
        string workstreamId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"RoleAttributes:AppRoleId:{appRoleId}:{workstreamId}";

        if (_cache.TryGetValue<RoleAttributes>(cacheKey, out var cached))
        {
            return cached;
        }

        var entity = await _context.RoleAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AppRoleId == appRoleId && r.WorkstreamId == workstreamId && r.IsActive, cancellationToken);

        if (entity == null)
        {
            _logger.LogDebug("Role attributes not found for app role ID {AppRoleId} in workstream {WorkstreamId}", appRoleId, workstreamId);
            return null;
        }

        var attributes = MapToModel(entity);
        _cache.Set(cacheKey, attributes, CacheDuration);

        return attributes;
    }

    public async Task<RoleAttributes?> GetAttributesByRoleValueAsync(
        string roleValue,
        string workstreamId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"RoleAttributes:RoleValue:{roleValue}:{workstreamId}";

        if (_cache.TryGetValue<RoleAttributes>(cacheKey, out var cached))
        {
            return cached;
        }

        var entity = await _context.RoleAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleValue == roleValue && r.WorkstreamId == workstreamId && r.IsActive, cancellationToken);

        if (entity == null)
        {
            _logger.LogDebug("Role attributes not found for role value {RoleValue} in workstream {WorkstreamId}", roleValue, workstreamId);
            return null;
        }

        var attributes = MapToModel(entity);
        _cache.Set(cacheKey, attributes, CacheDuration);

        // Also cache by AppRoleId
        var appRoleIdKey = $"RoleAttributes:AppRoleId:{entity.AppRoleId}:{workstreamId}";
        _cache.Set(appRoleIdKey, attributes, CacheDuration);

        return attributes;
    }

    public async Task<IDictionary<string, RoleAttributes>> GetAttributesByRoleValuesAsync(
        IEnumerable<string> roleValues,
        string workstreamId,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, RoleAttributes>();
        var uncachedValues = new List<string>();

        // Check cache first
        foreach (var roleValue in roleValues)
        {
            var cacheKey = $"RoleAttributes:RoleValue:{roleValue}:{workstreamId}";
            if (_cache.TryGetValue<RoleAttributes>(cacheKey, out var cached) && cached != null)
            {
                result[roleValue] = cached;
            }
            else
            {
                uncachedValues.Add(roleValue);
            }
        }

        // Fetch uncached from database
        if (uncachedValues.Count > 0)
        {
            var entities = await _context.RoleAttributes
                .AsNoTracking()
                .Where(r => uncachedValues.Contains(r.RoleValue) && r.WorkstreamId == workstreamId && r.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var entity in entities)
            {
                var attributes = MapToModel(entity);

                var roleValueKey = $"RoleAttributes:RoleValue:{entity.RoleValue}:{workstreamId}";
                var appRoleIdKey = $"RoleAttributes:AppRoleId:{entity.AppRoleId}:{workstreamId}";

                _cache.Set(roleValueKey, attributes, CacheDuration);
                _cache.Set(appRoleIdKey, attributes, CacheDuration);

                result[entity.RoleValue] = attributes;
            }
        }

        return result;
    }

    public async Task UpdateAttributesAsync(
        RoleAttributes attributes,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.RoleAttributes
            .FirstOrDefaultAsync(r => r.AppRoleId == attributes.AppRoleId && r.WorkstreamId == attributes.WorkstreamId, cancellationToken);

        if (entity == null)
        {
            entity = new Persistence.Entities.Authorization.RoleAttribute
            {
                AppRoleId = attributes.AppRoleId,
                WorkstreamId = attributes.WorkstreamId,
                RoleValue = attributes.RoleValue,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.RoleAttributes.Add(entity);
        }

        entity.RoleValue = attributes.RoleValue;
        entity.RoleDisplayName = attributes.RoleDisplayName;
        entity.AttributesJson = SerializeAttributes(attributes.Attributes);
        entity.ModifiedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var appRoleIdKey = $"RoleAttributes:AppRoleId:{attributes.AppRoleId}:{attributes.WorkstreamId}";
        var roleValueKey = $"RoleAttributes:RoleValue:{attributes.RoleValue}:{attributes.WorkstreamId}";
        _cache.Remove(appRoleIdKey);
        _cache.Remove(roleValueKey);

        _logger.LogInformation(
            "Updated role attributes for {AppRoleId} ({RoleValue}) in workstream {WorkstreamId}",
            attributes.AppRoleId, attributes.RoleValue, attributes.WorkstreamId);
    }

    private static RoleAttributes MapToModel(Persistence.Entities.Authorization.RoleAttribute entity)
    {
        return new RoleAttributes
        {
            AppRoleId = entity.AppRoleId,
            RoleValue = entity.RoleValue,
            WorkstreamId = entity.WorkstreamId,
            RoleDisplayName = entity.RoleDisplayName,
            Attributes = DeserializeAttributes(entity.AttributesJson)
        };
    }

    private static Dictionary<string, JsonElement> DeserializeAttributes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, JsonElement>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                ?? new Dictionary<string, JsonElement>();
        }
        catch
        {
            return new Dictionary<string, JsonElement>();
        }
    }

    private static string? SerializeAttributes(Dictionary<string, JsonElement>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return null;

        return JsonSerializer.Serialize(attributes);
    }
}
