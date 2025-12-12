namespace UI.Modules.AccessControl.Services.Attributes;

/// <summary>
/// Service for resolving global user attributes (JobTitle, Department) with fallback logic.
/// Implements precedence: UserAttributes (global) > RoleAttributes (global).
/// </summary>
public interface IGlobalAttributeService
{
    /// <summary>
    /// Gets the job title for a user.
    /// Checks UserAttributes with WorkstreamId='global' first, then falls back to RoleAttributes.
    /// </summary>
    /// <param name="userId">The user ID (Entra oid)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job title if found, null otherwise</returns>
    Task<string?> GetUserJobTitleAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the department for a user.
    /// Checks UserAttributes with WorkstreamId='global' first, then falls back to RoleAttributes.
    /// </summary>
    /// <param name="userId">The user ID (Entra oid)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Department if found, null otherwise</returns>
    Task<string?> GetUserDepartmentAsync(string userId, CancellationToken cancellationToken = default);
}
