namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Request model for batch authorization check endpoint.
/// Allows checking multiple resource/action combinations in a single API call.
/// </summary>
public class BatchAuthorizationCheckRequest
{
    /// <summary>
    /// The workstream context for all checks in this batch.
    /// </summary>
    public string WorkstreamId { get; set; } = string.Empty;

    /// <summary>
    /// List of resource/action checks to perform.
    /// </summary>
    public List<ResourceActionCheck> Checks { get; set; } = new();
}

/// <summary>
/// Represents a single resource/action combination to check.
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
