using System.Security.Claims;
using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Provides access to the current authenticated user.
/// </summary>
public class CurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    ICorrelationContextAccessor correlationContextAccessor,
    IUserAttributeStore userAttributeStore) : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ICorrelationContextAccessor _correlationContextAccessor = correlationContextAccessor;
    private readonly IUserAttributeStore _userAttributeStore = userAttributeStore;
    private CurrentUser? _cachedUser;

    public CurrentUser User
    {
        get
        {
            if (_cachedUser != null)
                return _cachedUser;

            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("HTTP context not available");

            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException("User is not authenticated");
            }

            // CRITICAL: After clearing DefaultInboundClaimTypeMap, claims retain their full URI names
            // The 'oid' claim from Entra ID comes as "http://schemas.microsoft.com/identity/claims/objectidentifier"
            // This is the stable, immutable user identifier recommended by Microsoft
            var userId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? throw new InvalidOperationException("User ID not found: 'oid' claim (http://schemas.microsoft.com/identity/claims/objectidentifier) is required for Entra ID authentication");

            var displayName = user.FindFirst("name")?.Value ?? "Unknown";
            var email = user.FindFirst("email")?.Value;

            // Get user attributes (synchronously - cached)
            var workstreamId = WorkstreamId;
            var attributes = _userAttributeStore.GetAttributesAsync(userId, workstreamId)
                .ConfigureAwait(false).GetAwaiter().GetResult();

            _cachedUser = new CurrentUser
            {
                Id = userId,
                DisplayName = displayName,
                Email = email,
                Type = UserType.User,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                WorkstreamId = workstreamId
            };

            return _cachedUser;
        }
    }

    public string WorkstreamId =>
        _correlationContextAccessor.Context?.WorkstreamId
        ?? throw new InvalidOperationException("Workstream context not available");
}
