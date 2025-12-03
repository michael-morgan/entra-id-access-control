using System.Security.Claims;
using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Builds ABAC context from current user, resource, and environment.
/// Merges attributes from user, groups, and roles with precedence: User > Role > Group.
/// </summary>
public class DefaultAbacContextProvider : IAbacContextProvider
{
    private readonly IUserAttributeStore _userAttributeStore;
    private readonly IGroupAttributeStore _groupAttributeStore;
    private readonly IRoleAttributeStore _roleAttributeStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<AuthorizationOptions> _options;

    public DefaultAbacContextProvider(
        IUserAttributeStore userAttributeStore,
        IGroupAttributeStore groupAttributeStore,
        IRoleAttributeStore roleAttributeStore,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuthorizationOptions> options)
    {
        _userAttributeStore = userAttributeStore;
        _groupAttributeStore = groupAttributeStore;
        _roleAttributeStore = roleAttributeStore;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    public async Task<AbacContext> BuildContextAsync(
        ClaimsPrincipal user,
        string workstreamId,
        string resource,
        string action,
        object? resourceEntity = null)
    {
        // CRITICAL: After clearing DefaultInboundClaimTypeMap, claims retain their full URI names
        // The 'oid' claim from Entra ID comes as "http://schemas.microsoft.com/identity/claims/objectidentifier"
        // This is the stable, immutable user identifier recommended by Microsoft
        var userId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? throw new InvalidOperationException("User ID not found: 'oid' claim (http://schemas.microsoft.com/identity/claims/objectidentifier) is required for Entra ID authentication");

        // Extract roles and groups from JWT claims
        var roleValues = user.FindAll("roles").Select(c => c.Value).ToArray();
        var groupIds = user.FindAll("groups").Select(c => c.Value).ToArray();

        // Load attributes from database sequentially (DbContext is not thread-safe)
        var userAttributes = await _userAttributeStore.GetAttributesAsync(userId, workstreamId);
        var groupAttributes = await _groupAttributeStore.GetAttributesAsync(groupIds, workstreamId);
        var roleAttributes = await _roleAttributeStore.GetAttributesByRoleValuesAsync(roleValues, workstreamId);

        // Merge attributes with precedence: User > Role > Group
        var mergedAttributes = MergeAttributes(groupAttributes, roleAttributes, userAttributes);

        // Build environment context
        var httpContext = _httpContextAccessor.HttpContext;
        var now = DateTimeOffset.UtcNow;
        var clientIp = httpContext?.Connection.RemoteIpAddress?.ToString();

        // Extract resource attributes dynamically
        var resourceAttributes = ExtractResourceAttributes(resourceEntity);

        var context = new AbacContext
        {
            // User attributes
            UserId = userId,
            UserDisplayName = user.FindFirst("name")?.Value,
            UserEmail = user.FindFirst("email")?.Value,
            Roles = roleValues,
            Groups = groupIds,

            // Merged attributes (User > Role > Group precedence)
            Department = mergedAttributes.GetString("Department"),
            Region = mergedAttributes.GetString("Region"),
            ApprovalLimit = mergedAttributes.GetDecimal("ApprovalLimit"),
            ManagementLevel = mergedAttributes.GetInt("ManagementLevel"),

            // Resource attributes (extracted from entity if provided)
            ResourceOwnerId = GetResourceProperty<string>(resourceEntity, "OwnerId")
                ?? GetResourceProperty<string>(resourceEntity, "CreatedBy"),
            ResourceRegion = GetResourceProperty<string>(resourceEntity, "Region"),
            ResourceStatus = GetResourceProperty<string>(resourceEntity, "Status"),
            ResourceValue = GetResourceProperty<decimal?>(resourceEntity, "RequestedAmount")
                ?? GetResourceProperty<decimal?>(resourceEntity, "Amount")
                ?? GetResourceProperty<decimal?>(resourceEntity, "Value"),
            ResourceClassification = GetResourceProperty<string>(resourceEntity, "Classification"),
            ResourceCreatedAt = GetResourceProperty<DateTimeOffset?>(resourceEntity, "CreatedAt"),
            ResourceAttributes = resourceAttributes,

            // Environment attributes
            RequestTime = now,
            ClientIpAddress = clientIp,
            IsBusinessHours = IsWithinBusinessHours(now),
            IsInternalNetwork = IsInternalNetwork(clientIp),

            CustomAttributes = new Dictionary<string, object>(mergedAttributes)
        };

        return context;
    }

    /// <summary>
    /// Merges attributes with precedence: User > Role > Group.
    /// User attributes override role attributes, which override group attributes.
    /// Returns a dictionary of all merged attributes.
    /// </summary>
    private static Dictionary<string, object> MergeAttributes(
        IDictionary<string, GroupAttributes> groupAttributes,
        IDictionary<string, RoleAttributes> roleAttributes,
        UserAttributes? userAttributes)
    {
        var result = new Dictionary<string, object>();

        // 1. Start with group attributes (lowest precedence)
        foreach (var group in groupAttributes.Values)
        {
            foreach (var attr in group.Attributes)
            {
                if (!result.ContainsKey(attr.Key))
                {
                    result[attr.Key] = ConvertJsonElement(attr.Value);
                }
            }
        }

        // 2. Override with role attributes (medium precedence)
        foreach (var role in roleAttributes.Values)
        {
            foreach (var attr in role.Attributes)
            {
                result[attr.Key] = ConvertJsonElement(attr.Value);
            }
        }

        // 3. Override with user attributes (highest precedence)
        if (userAttributes != null)
        {
            foreach (var attr in userAttributes.Attributes)
            {
                result[attr.Key] = ConvertJsonElement(attr.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// Converts JsonElement to appropriate .NET type.
    /// </summary>
    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : (element.TryGetInt64(out var l) ? l : element.GetDecimal()),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(e => ConvertJsonElement(e)).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.ToString()
        };
    }

    /// <summary>
    /// Extracts all properties from resource entity as a dictionary.
    /// </summary>
    private static Dictionary<string, object> ExtractResourceAttributes(object? resource)
    {
        var result = new Dictionary<string, object>();

        if (resource == null)
            return result;

        var properties = resource.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(resource);
            if (value != null)
            {
                result[property.Name] = value;
            }
        }

        return result;
    }

    private static T? GetResourceProperty<T>(object? resource, string propertyName)
    {
        if (resource == null)
            return default;

        var property = resource.GetType().GetProperty(propertyName);
        if (property == null)
            return default;

        var value = property.GetValue(resource);
        if (value == null)
            return default;

        // Handle enum-to-string conversion for generic string properties
        if (typeof(T) == typeof(string) && value.GetType().IsEnum)
        {
            return (T)(object)value.ToString()!;
        }

        return (T)value;
    }

    private bool IsWithinBusinessHours(DateTimeOffset time)
    {
        var options = _options.Value;
        var localTime = time.ToLocalTime();
        var hour = localTime.Hour;

        return hour >= options.BusinessHoursStart && hour < options.BusinessHoursEnd;
    }

    private bool IsInternalNetwork(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        var options = _options.Value;

        // Check against configured internal network ranges
        foreach (var range in options.InternalNetworkRanges)
        {
            if (ipAddress.StartsWith(range))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Authorization configuration options.
/// </summary>
public class AuthorizationOptions
{
    public TimeSpan PolicyCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PolicySlidingExpiration { get; set; } = TimeSpan.FromMinutes(1);
    public int BusinessHoursStart { get; set; } = 8;
    public int BusinessHoursEnd { get; set; } = 18;
    public List<string> InternalNetworkRanges { get; set; } = new() { "10.", "192.168." };
}

/// <summary>
/// Extension methods for attribute dictionaries.
/// </summary>
public static class AttributeDictionaryExtensions
{
    public static string? GetString(this Dictionary<string, object> attributes, string key)
    {
        return attributes.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    public static decimal? GetDecimal(this Dictionary<string, object> attributes, string key)
    {
        if (!attributes.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double db => (decimal)db,
            float f => (decimal)f,
            string s when decimal.TryParse(s, out var result) => result,
            _ => null
        };
    }

    public static int? GetInt(this Dictionary<string, object> attributes, string key)
    {
        if (!attributes.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            double db => (int)db,
            float f => (int)f,
            string s when int.TryParse(s, out var result) => result,
            _ => null
        };
    }

    public static bool? GetBoolean(this Dictionary<string, object> attributes, string key)
    {
        if (!attributes.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var result) => result,
            _ => null
        };
    }
}
