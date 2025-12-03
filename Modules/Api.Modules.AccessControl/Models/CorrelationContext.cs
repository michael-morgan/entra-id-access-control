namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Encapsulates all correlation identifiers for the current request.
/// Immutable record for thread-safety.
/// </summary>
public sealed record CorrelationContext
{
    /// <summary>
    /// Long-lived identifier for a business workflow spanning sessions/days.
    /// Null for standalone requests not associated with a business process.
    /// Example: "LoanApplication-2024-00123", "Claim-2024-56789"
    /// </summary>
    public string? BusinessProcessId { get; init; }

    /// <summary>
    /// Identifies a user's logical session across multiple requests.
    /// Survives page refreshes, spans multiple API calls within one browser session.
    /// Null if not provided by frontend.
    /// </summary>
    public string? SessionCorrelationId { get; init; }

    /// <summary>
    /// Unique identifier for this specific HTTP request.
    /// Used for distributed tracing and log correlation.
    /// Always present - generated if not provided.
    /// </summary>
    public required string RequestCorrelationId { get; init; }

    /// <summary>
    /// The workstream/module that initiated this context.
    /// Used for policy scoping and event attribution.
    /// </summary>
    public required string WorkstreamId { get; init; }

    /// <summary>
    /// UTC timestamp when the request was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional parent span ID for distributed tracing.
    /// </summary>
    public string? ParentSpanId { get; init; }
}
