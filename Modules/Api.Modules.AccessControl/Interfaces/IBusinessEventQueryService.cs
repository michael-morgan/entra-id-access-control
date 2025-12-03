using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Query service for business events.
/// </summary>
public interface IBusinessEventQueryService
{
    /// <summary>
    /// Queries events with filtering and paging.
    /// </summary>
    Task<PagedResult<BusinessEventSummary>> QueryAsync(
        EventQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the timeline of events for a business process.
    /// </summary>
    Task<IReadOnlyList<BusinessEventSummary>> GetProcessTimelineAsync(
        string businessProcessId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific event.
    /// </summary>
    Task<BusinessEventDetail?> GetEventDetailAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events related to a specific entity.
    /// </summary>
    Task<IReadOnlyList<BusinessEventSummary>> GetEntityEventsAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);
}
