using Api.Modules.AccessControl.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Authorization handler for resource-based policies using Casbin.
/// </summary>
public class CasbinAuthorizationHandler(
    IAuthorizationEnforcer enforcer,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CasbinAuthorizationHandler> logger) : IAuthorizationHandler
{
    private readonly IAuthorizationEnforcer _enforcer = enforcer;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<CasbinAuthorizationHandler> _logger = logger;

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.ToList();

        foreach (var requirement in pendingRequirements)
        {
            // Check if this is a resource-based requirement
            if (requirement is ResourceAuthorizationRequirement resourceRequirement)
            {
                var policyName = resourceRequirement.PolicyName;

                // Extract resource and action from policy name
                // Format: Resource:{resource}:Action:{action}
                if (policyName.StartsWith("Resource:", StringComparison.OrdinalIgnoreCase))
                {
                    // Find :Action: marker to split resource from action
                    const string actionMarker = ":Action:";
                    var actionIndex = policyName.IndexOf(actionMarker, StringComparison.OrdinalIgnoreCase);

                    if (actionIndex > 0)
                    {
                        var resource = policyName["Resource:".Length..actionIndex];
                        var action = policyName[(actionIndex + actionMarker.Length)..];

                        // Replace route parameters in resource
                        resource = ReplaceRouteParameters(resource);

                        var result = await _enforcer.CheckAsync(resource, action, null);

                        if (result.IsAllowed)
                        {
                            context.Succeed(requirement);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Authorization failed for policy {Policy}: {Reason}",
                                policyName, result.DenialReason);
                            context.Fail();
                        }
                    }
                }
            }
        }
    }

    private string ReplaceRouteParameters(string resource)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return resource;

        // Get route data
        var routeData = httpContext.GetRouteData();
        if (routeData == null)
            return resource;

        // Replace :id with actual route value
        if (resource.Contains(":id"))
        {
            var id = routeData.Values["id"]?.ToString();
            if (id != null)
            {
                resource = resource.Replace(":id", id);
            }
        }

        // Replace other common route parameters
        foreach (var routeValue in routeData.Values)
        {
            var placeholder = $":{routeValue.Key}";
            if (resource.Contains(placeholder))
            {
                resource = resource.Replace(placeholder, routeValue.Value?.ToString() ?? "");
            }
        }

        return resource;
    }
}
