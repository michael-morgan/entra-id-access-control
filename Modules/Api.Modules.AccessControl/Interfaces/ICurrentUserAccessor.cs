using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Provides access to the current authenticated user.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Gets the current user.
    /// </summary>
    CurrentUser User { get; }

    /// <summary>
    /// Gets the workstream ID for the current request.
    /// </summary>
    string WorkstreamId { get; }

    /// <summary>
    /// Gets the regions the user has access to.
    /// </summary>
    string[] Regions { get; }

    /// <summary>
    /// Whether the user has global access (bypasses row-level filters).
    /// </summary>
    bool HasGlobalAccess { get; }
}
