namespace Api.Modules.AccessControl.Correlation;

/// <summary>
/// Configuration options for correlation framework.
/// </summary>
public class CorrelationOptions
{
    /// <summary>
    /// Default workstream ID when header is not provided.
    /// </summary>
    public string? DefaultWorkstreamId { get; set; }

    /// <summary>
    /// Whether to include correlation IDs in response headers.
    /// Default: true
    /// </summary>
    public bool IncludeInResponse { get; set; } = true;

    /// <summary>
    /// Whether to generate request IDs if not provided.
    /// Default: true
    /// </summary>
    public bool GenerateRequestIdIfMissing { get; set; } = true;

    /// <summary>
    /// Whether to integrate with W3C Trace Context.
    /// Default: true
    /// </summary>
    public bool UseW3CTraceContext { get; set; } = true;
}
