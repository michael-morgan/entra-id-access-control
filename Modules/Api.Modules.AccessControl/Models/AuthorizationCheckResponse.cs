namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Response model for authorization check endpoint.
/// </summary>
public class AuthorizationCheckResponse
{
    /// <summary>
    /// The resource that was checked.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// The action that was checked.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is authorized to perform the action on the resource.
    /// </summary>
    public bool Allowed { get; set; }

    /// <summary>
    /// Reason for denial if not allowed (null if allowed).
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// The workstream context used for this authorization check.
    /// </summary>
    public string WorkstreamId { get; set; } = string.Empty;
}
