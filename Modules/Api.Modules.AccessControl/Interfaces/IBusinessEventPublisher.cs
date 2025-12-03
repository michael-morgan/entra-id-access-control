using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Publishes business events to the event store.
/// </summary>
public interface IBusinessEventPublisher
{
    /// <summary>
    /// Publishes an event without justification.
    /// </summary>
    Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : BusinessEvent;

    /// <summary>
    /// Publishes an event with justification (for approvals, overrides).
    /// </summary>
    Task PublishAsync<TEvent>(
        TEvent @event,
        string? justification,
        CancellationToken cancellationToken = default)
        where TEvent : BusinessEvent;

    /// <summary>
    /// Publishes multiple events atomically.
    /// </summary>
    Task PublishManyAsync(
        IEnumerable<BusinessEvent> events,
        CancellationToken cancellationToken = default);
}
