using System.Security.Claims;
using Api.Modules.AccessControl.Configuration;
using Api.Modules.AccessControl.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Middleware;

/// <summary>
/// Middleware that synchronizes user group memberships from JWT tokens to the database.
/// Runs after authentication, before authorization.
/// Operates in fire-and-forget mode to avoid blocking the request pipeline.
/// </summary>
/// <remarks>
/// IMPORTANT: This middleware is for UI display purposes only.
/// Authorization decisions ALWAYS use JWT groups directly (source of truth).
/// Database groups are enriched with friendly names for admin UI convenience.
///
/// Performance: Uses in-memory caching to limit DB writes (configurable duration).
/// Error Handling: Logs errors but doesn't throw - won't block requests if sync fails.
/// </remarks>
public class GroupSyncMiddleware(
    RequestDelegate next,
    ILogger<GroupSyncMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GroupSyncMiddleware> _logger = logger;

    public async Task InvokeAsync(
        HttpContext context,
        IOptions<GroupSyncOptions> options)
    {
        try
        {
            // Check if user is authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Check if group sync is enabled
                if (options.Value.EnableJwtGroupSync)
                {
                    // Capture user claims and service provider for background task
                    var userClaims = context.User.Clone();
                    var serviceProvider = context.RequestServices;

                    // Fire-and-forget: Don't await to avoid blocking the request
                    // Create a new DI scope to avoid DbContext concurrency issues
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = serviceProvider.CreateScope();
                            var groupSyncService = scope.ServiceProvider.GetRequiredService<IJwtGroupSyncService>();
                            await groupSyncService.SyncUserGroupsFromJwtAsync(userClaims);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Background group sync failed for user");
                        }
                    });

                    _logger.LogDebug("Group sync initiated for authenticated user");
                }
                else
                {
                    _logger.LogDebug("Group sync is disabled via configuration");
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - middleware failures shouldn't block requests
            _logger.LogError(ex, "Error in GroupSyncMiddleware");
        }

        // Continue pipeline
        await _next(context);
    }
}
