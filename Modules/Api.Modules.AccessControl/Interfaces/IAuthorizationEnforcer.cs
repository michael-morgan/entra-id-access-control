using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Programmatic authorization enforcer for service layer.
/// Use when entity data is needed for ABAC evaluation.
/// </summary>
public interface IAuthorizationEnforcer
{
    /// <summary>
    /// Checks authorization and returns result without throwing.
    /// </summary>
    Task<AuthorizationResult> CheckAsync(
        string resource,
        string action,
        object? resourceEntity = null);

    /// <summary>
    /// Checks authorization for a specific entity.
    /// </summary>
    Task<AuthorizationResult> CheckAsync<TEntity>(
        TEntity entity,
        string action) where TEntity : class;

    /// <summary>
    /// Checks authorization and throws if denied.
    /// </summary>
    Task EnsureAuthorizedAsync(
        string resource,
        string action,
        object? resourceEntity = null);

    /// <summary>
    /// Checks authorization for entity and throws if denied.
    /// </summary>
    Task EnsureAuthorizedAsync<TEntity>(
        TEntity entity,
        string action) where TEntity : class;
}
