namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Abstraction for policy enforcement engine.
/// Decouples the framework from specific policy engine implementations (e.g., Casbin).
/// </summary>
public interface IPolicyEngine
{
    /// <summary>
    /// Evaluates whether a subject is allowed to perform an action on a resource.
    /// </summary>
    /// <param name="subject">Subject (user ID or group ID)</param>
    /// <param name="workstream">Workstream identifier</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="action">Action being performed</param>
    /// <param name="context">Additional context (e.g., ABAC context JSON)</param>
    /// <returns>True if access is allowed, false otherwise</returns>
    bool Enforce(string subject, string workstream, string resource, string action, string context);

    /// <summary>
    /// Gets all roles for a subject within a workstream, including inherited roles.
    /// Resolves the complete role hierarchy using recursive Casbin policy traversal.
    /// Results are cached to improve performance.
    /// </summary>
    /// <param name="subject">Subject (user ID or group ID)</param>
    /// <param name="workstream">Workstream identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all roles (direct and inherited) for the subject</returns>
    Task<IEnumerable<string>> GetAllRolesForSubjectAsync(string subject, string workstream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a subject has a specific role in a workstream.
    /// </summary>
    /// <param name="subject">Subject (user ID or group ID)</param>
    /// <param name="role">Role name</param>
    /// <param name="workstream">Workstream identifier</param>
    /// <returns>True if subject has the role, false otherwise</returns>
    bool HasRole(string subject, string role, string workstream);

    /// <summary>
    /// Gets all subjects (users/groups) that have a specific role in a workstream.
    /// </summary>
    /// <param name="role">Role name</param>
    /// <param name="workstream">Workstream identifier</param>
    /// <returns>Collection of subject IDs</returns>
    IEnumerable<string> GetSubjectsForRole(string role, string workstream);
}
