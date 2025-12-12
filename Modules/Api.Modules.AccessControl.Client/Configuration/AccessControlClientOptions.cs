namespace Api.Modules.AccessControl.Client.Configuration;

/// <summary>
/// Configuration options for the AccessControl client SDK.
/// </summary>
public class AccessControlClientOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AccessControl:Client";

    /// <summary>
    /// Base URL of the AccessControl API (modular monolith).
    /// Example: "https://api.yourcompany.com"
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default workstream ID to use if not specified in requests.
    /// Example: "loans", "claims", "platform"
    /// </summary>
    public string? DefaultWorkstreamId { get; set; }

    /// <summary>
    /// Whether to enable Redis caching for authorization decisions.
    /// Default: false
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// Redis connection string (required if EnableCaching is true).
    /// Example: "localhost:6379,ssl=false,abortConnect=false"
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Cache expiration time in seconds.
    /// Default: 86400 (24 hours)
    /// </summary>
    public int CacheExpirationSeconds { get; set; } = 86400;

    /// <summary>
    /// HTTP request timeout in seconds.
    /// Default: 30
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to forward the user's JWT token to the API.
    /// Default: true (required for authorization checks)
    /// </summary>
    public bool ForwardJwtToken { get; set; } = true;

    /// <summary>
    /// Number of retry attempts for transient failures.
    /// Default: 3
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Exponential backoff base delay in milliseconds.
    /// Default: 100
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 100;
}
