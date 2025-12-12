namespace Api.Modules.AccessControl.Client.Models;

/// <summary>
/// Request model for batch authorization check endpoint.
/// Checks multiple resource/action combinations in a single request.
/// </summary>
public class BatchAuthorizationCheckRequest
{
    /// <summary>
    /// The workstream context for all checks.
    /// </summary>
    public string WorkstreamId { get; set; } = string.Empty;

    /// <summary>
    /// List of resource/action combinations to check.
    /// </summary>
    public List<ResourceActionCheck> Checks { get; set; } = new();
}

/// <summary>
/// A single resource/action combination to check.
/// </summary>
public class ResourceActionCheck
{
    /// <summary>
    /// The resource being accessed.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// The action being performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;
}
