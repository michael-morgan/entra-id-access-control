namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Represents a long-lived business process.
/// </summary>
public record BusinessProcess
{
    public required string BusinessProcessId { get; init; }
    public required string ProcessType { get; init; }
    public required string WorkstreamId { get; init; }
    public required BusinessProcessStatus Status { get; init; }
    public required string InitiatedBy { get; init; }
    public required DateTimeOffset InitiatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public BusinessProcessOutcome? Outcome { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Business process lifecycle status.
/// </summary>
public enum BusinessProcessStatus
{
    Active,
    Completed,
    Cancelled,
    Suspended
}

/// <summary>
/// Business process terminal outcomes.
/// </summary>
public enum BusinessProcessOutcome
{
    Approved,
    Denied,
    Withdrawn,
    Expired,
    Error
}
