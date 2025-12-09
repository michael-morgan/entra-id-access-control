using Api.Modules.AccessControl.Interfaces;
using Casbin;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Casbin implementation of IPolicyEngine.
/// Adapts Casbin's IEnforcer to our framework's abstraction.
/// </summary>
public class CasbinPolicyEngine(IEnforcer enforcer) : IPolicyEngine
{
    private readonly IEnforcer _enforcer = enforcer;

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
}
