namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Query parameters for business events.
/// </summary>
public record EventQuery
{
    public string? WorkstreamId { get; init; }
    public string? BusinessProcessId { get; init; }
    public string? EventType { get; init; }
    public string? EventCategory { get; init; }
    public string? ActorId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string SortBy { get; init; } = "OccurredAt";
    public bool SortDescending { get; init; } = true;
}

/// <summary>
/// Paged result wrapper.
/// </summary>
public record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Summary of a business event for list views.
/// </summary>
public record BusinessEventSummary(
    Guid EventId,
    string EventType,
    string EventCategory,
    string ActorDisplayName,
    DateTimeOffset OccurredAt,
    string WorkstreamId,
    string? BusinessProcessId);

/// <summary>
/// Detailed business event information.
/// </summary>
public record BusinessEventDetail(
    Guid EventId,
    long SequenceNumber,
    string EventType,
    string EventCategory,
    int EventVersion,
    string? BusinessProcessId,
    string WorkstreamId,
    string ActorId,
    string ActorType,
    string ActorDisplayName,
    DateTimeOffset OccurredAt,
    DateTimeOffset RecordedAt,
    string EventDataJson,
    string? Justification,
    IReadOnlyList<AffectedEntity> AffectedEntities);
