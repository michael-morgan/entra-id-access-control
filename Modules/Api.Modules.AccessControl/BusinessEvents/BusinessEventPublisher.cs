using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.BusinessEvents;

/// <summary>
/// Publishes business events to the event store.
/// Simplified version without outbox pattern - direct writes only.
/// </summary>
public class BusinessEventPublisher(
    IBusinessEventStore eventStore,
    ILogger<BusinessEventPublisher> logger) : IBusinessEventPublisher
{
    private readonly IBusinessEventStore _eventStore = eventStore;
    private readonly ILogger<BusinessEventPublisher> _logger = logger;

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : BusinessEvent
    {
        await PublishAsync(@event, null, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        string? justification,
        CancellationToken cancellationToken = default)
        where TEvent : BusinessEvent
    {
        await _eventStore.StoreAsync(@event, justification, cancellationToken);

        _logger.LogInformation(
            "Business event published: {EventType}",
            @event.EventType);
    }

    public async Task PublishManyAsync(
        IEnumerable<BusinessEvent> events,
        CancellationToken cancellationToken = default)
    {
        await _eventStore.StoreManyAsync(events, cancellationToken);

        _logger.LogInformation(
            "Published {Count} business events",
            events.Count());
    }
}
