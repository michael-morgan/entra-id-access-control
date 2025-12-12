using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Client.Authorization;

/// <summary>
/// Custom policy provider that creates resource-based policies on-the-fly.
/// Allows using [Authorize(Policy="Loan/*:approve")] or [Authorize(Policy="Loan/*:approve:loans")].
/// Policy format: "{resource}:{action}" or "{resource}:{action}:{workstream}"
/// </summary>
public class AccessControlPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;
    private const char PolicySeparator = ':';

    public AccessControlPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if policy name matches our resource:action format
        var parts = policyName.Split(PolicySeparator);

        if (parts.Length >= 2 && parts.Length <= 3)
        {
            var resource = parts[0];
            var action = parts[1];
            var workstream = parts.Length == 3 ? parts[2] : null;

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ResourceAuthorizationRequirement(resource, action, workstream))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Not our format, fall back to default provider
        return _fallbackProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackProvider.GetFallbackPolicyAsync();
    }
}
