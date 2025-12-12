using Api.Modules.AccessControl.Client.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Client.Authorization;

/// <summary>
/// Authorization handler that delegates to the AccessControl API.
/// Handles ResourceAuthorizationRequirement by calling the centralized authorization service.
/// </summary>
public class ResourceAuthorizationHandler : AuthorizationHandler<ResourceAuthorizationRequirement>
{
    private readonly IAccessControlClient _client;
    private readonly ILogger<ResourceAuthorizationHandler> _logger;

    public ResourceAuthorizationHandler(
        IAccessControlClient client,
        ILogger<ResourceAuthorizationHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAuthorizationRequirement requirement)
    {
        try
        {
            _logger.LogDebug(
                "Checking authorization: Resource={Resource}, Action={Action}, Workstream={Workstream}",
                requirement.Resource,
                requirement.Action,
                requirement.WorkstreamId
            );

            var isAuthorized = await _client.IsAuthorizedAsync(
                requirement.Resource,
                requirement.Action,
                requirement.WorkstreamId
            );

            if (isAuthorized)
            {
                _logger.LogInformation(
                    "Authorization ALLOWED: Resource={Resource}, Action={Action}",
                    requirement.Resource,
                    requirement.Action
                );
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning(
                    "Authorization DENIED: Resource={Resource}, Action={Action}",
                    requirement.Resource,
                    requirement.Action
                );
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking authorization: Resource={Resource}, Action={Action}",
                requirement.Resource,
                requirement.Action
            );

            // Fail-secure: deny on error
            context.Fail();
        }
    }
}
