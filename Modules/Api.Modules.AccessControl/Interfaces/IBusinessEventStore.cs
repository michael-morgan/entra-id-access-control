using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Stores and retrieves business events.
/// </summary>
public interface IBusinessEventStore
{
    /// <summary>
    /// Stores a single event.
    /// </summary>
    Task<Guid> StoreAsync<TEvent>(
        TEvent @event,
        string? justification,
        CancellationToken cancellationToken)
        where TEvent : BusinessEvent;

    /// <summary>
    /// Stores multiple events atomically.
    /// </summary>
    Task<IReadOnlyList<Guid>> StoreManyAsync(
        IEnumerable<BusinessEvent> events,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves events for a business process.
    /// </summary>
    Task<IReadOnlyList<BusinessEventSummary>> GetProcessEventsAsync(
        string businessProcessId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Queries events with filtering.
    /// </summary>
    Task<PagedResult<BusinessEventSummary>> QueryAsync(
        EventQuery query,
        CancellationToken cancellationToken);
}
