using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

namespace UI.Modules.AccessControl.Middleware;

/// <summary>
/// Middleware that validates token cache availability on each request.
/// If token cache is empty (e.g., after app restart), redirects to sign-in proactively.
/// This prevents errors when pages later try to call Microsoft Graph API.
/// </summary>
public class TokenCacheValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenCacheValidationMiddleware> _logger;

    public TokenCacheValidationMiddleware(
        RequestDelegate next,
        ILogger<TokenCacheValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenAcquisition tokenAcquisition)
    {
        // Only check for authenticated users
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Skip token validation for certain paths (static files, sign-in callback, etc.)
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/signin-oidc", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/signout-", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/MicrosoftIdentity/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            try
            {
                // Try to acquire a token silently to verify cache is valid
                // Use minimal scopes - just need to verify token cache works
                var scopes = new[] { "User.Read" };
                await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            }
            catch (MicrosoftIdentityWebChallengeUserException)
            {
                // Token cache is empty - redirect to sign-in
                _logger.LogWarning("Token cache empty, redirecting to sign-in. Original URL: {Url}",
                    context.Request.Path + context.Request.QueryString);

                var returnUrl = context.Request.Path + context.Request.QueryString;

                await context.ChallengeAsync(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    new AuthenticationProperties
                    {
                        RedirectUri = returnUrl
                    });
                return;
            }
        }

        await _next(context);
    }
}
