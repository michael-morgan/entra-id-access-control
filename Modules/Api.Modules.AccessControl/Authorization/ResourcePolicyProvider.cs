using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Custom authorization policy provider that dynamically creates policies for resource-based authorization.
/// Handles policies with names like "Resource:Loan:Action:list" created by AuthorizeResourceAttribute.
/// </summary>
public class ResourcePolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public ResourcePolicyProvider(IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if this is a resource-based policy (format: "Resource:{resource}:Action:{action}")
        if (policyName.StartsWith("Resource:", StringComparison.OrdinalIgnoreCase))
        {
            // Create a policy that requires the CasbinAuthorizationHandler
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ResourceAuthorizationRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Fall back to default policy provider for non-resource policies
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// Authorization requirement for resource-based policies.
/// Used by the CasbinAuthorizationHandler to enforce policies.
/// </summary>
public class ResourceAuthorizationRequirement : IAuthorizationRequirement
{
    public string PolicyName { get; }

    public ResourceAuthorizationRequirement(string policyName)
    {
        PolicyName = policyName;
    }
}
