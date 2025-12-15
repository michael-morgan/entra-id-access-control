using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Casbin;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Casbin implementation of IPolicyEngine.
/// Adapts Casbin's IEnforcer to our framework's abstraction.
/// </summary>
public class CasbinPolicyEngine(
    IEnforcer enforcer,
    IPolicyRepository policyRepository,
    IRoleResolutionCache roleResolutionCache,
    ILogger<CasbinPolicyEngine> logger) : IPolicyEngine
{
    private readonly IEnforcer _enforcer = enforcer;
    private readonly IPolicyRepository _policyRepository = policyRepository;
    private readonly IRoleResolutionCache _roleResolutionCache = roleResolutionCache;
    private readonly ILogger<CasbinPolicyEngine> _logger = logger;

    /// <inheritdoc/>
    public bool Enforce(string subject, string workstream, string resource, string action, string context)
    {
        return _enforcer.Enforce(subject, workstream, resource, action, context);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetRolesForSubject(string subject, string workstream)
    {
        // Casbin's GetRolesForUser returns roles for a subject in a domain (workstream)
        return _enforcer.GetRolesForUser(subject, workstream);
    }

    /// <inheritdoc/>
    public bool HasRole(string subject, string role, string workstream)
    {
        return _enforcer.HasRoleForUser(subject, role, workstream);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetSubjectsForRole(string role, string workstream)
    {
        return _enforcer.GetUsersForRole(role, workstream);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetAllRolesForSubjectAsync(string subject, string workstream, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cachedRoles = await _roleResolutionCache.GetAsync(subject, workstream, cancellationToken);
        if (cachedRoles != null)
        {
            _logger.LogDebug("[ROLE RESOLUTION] Cache hit for subject={Subject}, workstream={Workstream}", subject, workstream);
            return cachedRoles;
        }

        _logger.LogDebug("[ROLE RESOLUTION] Cache miss for subject={Subject}, workstream={Workstream}, resolving from policies...", subject, workstream);

        // Resolve roles recursively
        var allRoles = new List<string>();
        var visited = new HashSet<string>();

        await ResolveRolesRecursivelyAsync(subject, workstream, allRoles, visited, cancellationToken);

        _logger.LogDebug("[ROLE RESOLUTION] Resolved {Count} roles for subject={Subject}, workstream={Workstream}: {Roles}",
            allRoles.Count, subject, workstream, string.Join(", ", allRoles));

        // Cache the results
        await _roleResolutionCache.SetAsync(subject, workstream, allRoles, cancellationToken);

        return allRoles;
    }

    /// <summary>
    /// Recursively resolves all roles for a subject, including inherited roles.
    /// Uses depth-first traversal with cycle detection.
    /// </summary>
    private async Task ResolveRolesRecursivelyAsync(
        string subject,
        string workstream,
        List<string> allRoles,
        HashSet<string> visited,
        CancellationToken cancellationToken)
    {
        // Prevent infinite loops in case of circular role dependencies
        if (!visited.Add(subject))
        {
            _logger.LogWarning("[ROLE RESOLUTION] Circular dependency detected for subject={Subject}, workstream={Workstream}", subject, workstream);
            return;
        }

        // Get direct roles from policies (batch query)
        var policies = await _policyRepository.GetRolesForSubjectsAsync(new[] { subject }, workstream, cancellationToken);

        foreach (var policy in policies)
        {
            var role = policy.V1; // V1 contains the role name in 'g' policies
            if (string.IsNullOrWhiteSpace(role))
                continue;

            // Add the role if not already present
            if (!allRoles.Contains(role))
            {
                allRoles.Add(role);
                _logger.LogDebug("[ROLE RESOLUTION] Added role={Role} for subject={Subject}, workstream={Workstream}", role, subject, workstream);

                // Recursively resolve inherited roles (role-to-role inheritance)
                await ResolveRolesRecursivelyAsync(role, workstream, allRoles, visited, cancellationToken);
            }
        }
    }
}
