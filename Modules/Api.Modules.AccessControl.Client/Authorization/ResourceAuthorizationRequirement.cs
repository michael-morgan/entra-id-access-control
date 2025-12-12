using Microsoft.AspNetCore.Authorization;

namespace Api.Modules.AccessControl.Client.Authorization;

/// <summary>
/// Authorization requirement for resource-based authorization.
/// Used with [Authorize(Policy="resource:action:workstream")] attribute.
/// </summary>
public class ResourceAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The resource being accessed.
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// The action being performed.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Optional workstream ID.
    /// </summary>
    public string? WorkstreamId { get; }

    public ResourceAuthorizationRequirement(string resource, string action, string? workstreamId = null)
    {
        Resource = resource;
        Action = action;
        WorkstreamId = workstreamId;
    }
}
