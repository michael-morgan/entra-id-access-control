using System.Security.Claims;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Builds ABAC context from current user, resource, and environment.
/// Delegates specialized tasks to dedicated services following SRP.
/// </summary>
public class DefaultAbacContextProvider(
    IUserAttributeStore userAttributeStore,
    IGroupAttributeStore groupAttributeStore,
    IRoleAttributeStore roleAttributeStore,
    IHttpContextAccessor httpContextAccessor,
    IAttributeMerger attributeMerger,
    IResourceAttributeExtractor resourceAttributeExtractor,
    IEnvironmentContextProvider environmentContextProvider) : IAbacContextProvider
{
    private readonly IUserAttributeStore _userAttributeStore = userAttributeStore;
    private readonly IGroupAttributeStore _groupAttributeStore = groupAttributeStore;
    private readonly IRoleAttributeStore _roleAttributeStore = roleAttributeStore;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IAttributeMerger _attributeMerger = attributeMerger;
    private readonly IResourceAttributeExtractor _resourceAttributeExtractor = resourceAttributeExtractor;
    private readonly IEnvironmentContextProvider _environmentContextProvider = environmentContextProvider;

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

        // Merge attributes with precedence: User > Role > Group (delegated to AttributeMerger)
        var mergedAttributes = _attributeMerger.MergeAttributes(groupAttributes, roleAttributes, userAttributes);

        // Build environment context (delegated to EnvironmentContextProvider)
        var httpContext = _httpContextAccessor.HttpContext;
        var now = DateTimeOffset.UtcNow;
        var clientIp = httpContext?.Connection.RemoteIpAddress?.ToString();

        // Extract resource attributes dynamically (delegated to ResourceAttributeExtractor)
        var resourceAttributes = _resourceAttributeExtractor.ExtractAttributes(resourceEntity);

        var context = new AbacContext
        {
            // User identity (from JWT)
            UserId = userId,
            UserDisplayName = user.FindFirst("name")?.Value,
            UserEmail = user.FindFirst("email")?.Value,
            Roles = roleValues,
            Groups = groupIds,

            // Dynamic user attributes (merged from User/Role/Group attributes)
            UserAttributes = mergedAttributes,

            // Dynamic resource attributes (extracted from entity if provided)
            ResourceAttributes = resourceAttributes,

            // Environment attributes (computed at runtime)
            RequestTime = now,
            ClientIpAddress = clientIp,
            IsBusinessHours = _environmentContextProvider.IsWithinBusinessHours(now),
            IsInternalNetwork = _environmentContextProvider.IsInternalNetwork(clientIp)
        };

        return context;
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
    public List<string> InternalNetworkRanges { get; set; } = ["10.", "192.168."];
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
