using System.Security.Claims;
using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Builds ABAC context from user, resource, and environment.
/// </summary>
public interface IAbacContextProvider
{
    /// <summary>
    /// Builds the runtime context for ABAC evaluation.
    /// </summary>
    Task<AbacContext> BuildContextAsync(
        ClaimsPrincipal user,
        string workstreamId,
        string resource,
        string action,
        object? resourceEntity = null);
}
