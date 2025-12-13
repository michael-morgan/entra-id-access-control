using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Client.Http;

/// <summary>
/// HTTP message handler that adds JWT access tokens to requests sent to the AccessControl API.
/// Supports two strategies: custom token provider or HttpContext header forwarding.
/// </summary>
public class AccessControlTokenHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider? _tokenProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AccessControlTokenHandler> _logger;

    public AccessControlTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AccessControlTokenHandler> logger,
        IAccessTokenProvider? tokenProvider = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Try custom token provider first (for OIDC/cookie apps)
        if (_tokenProvider != null)
        {
            try
            {
                var token = await _tokenProvider.GetAccessTokenAsync();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug("JWT token from custom provider added to AccessControl API request");
                    return await base.SendAsync(request, cancellationToken);
                }

                _logger.LogWarning("Custom token provider returned null or empty token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Custom token provider failed");
                // Don't fall back - if custom provider is registered, it should work
                throw;
            }
        }

        // Fall back to HttpContext header forwarding (for API-to-API calls with JWT in header)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, cannot forward JWT token to AccessControl API");
            return await base.SendAsync(request, cancellationToken);
        }

        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            _logger.LogWarning("Authorization header not found in current request");
            return await base.SendAsync(request, cancellationToken);
        }

        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        _logger.LogDebug("JWT token from HttpContext forwarded to AccessControl API");

        return await base.SendAsync(request, cancellationToken);
    }
}
