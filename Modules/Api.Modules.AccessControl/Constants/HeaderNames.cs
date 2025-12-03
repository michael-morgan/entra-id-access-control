namespace Api.Modules.AccessControl.Constants;

/// <summary>
/// Custom HTTP header names for correlation tracking.
/// </summary>
public static class HeaderNames
{
    /// <summary>
    /// Long-lived business process identifier (e.g., "LoanApplication-2024-00123").
    /// </summary>
    public const string BusinessProcessId = "X-Business-Process-Id";

    /// <summary>
    /// Session-level correlation ID generated once per browser session.
    /// </summary>
    public const string SessionCorrelationId = "X-Session-Correlation-Id";

    /// <summary>
    /// Request-level correlation ID for distributed tracing.
    /// </summary>
    public const string RequestId = "X-Request-Id";

    /// <summary>
    /// Workstream identifier for policy scoping.
    /// </summary>
    public const string WorkstreamId = "X-Workstream-Id";
}
