using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Api.Modules.AccessControl.Persistence.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.BusinessEvents;

/// <summary>
/// Stores business events in SQL Server with immutability enforced by trigger.
/// NO hash chains - simplified version as per requirements.
/// </summary>
public class BusinessEventStore : IBusinessEventStore
{
    private readonly AccessControlDbContext _context;
    private readonly ICorrelationContextAccessor _correlation;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<BusinessEventStore> _logger;

    public BusinessEventStore(
        AccessControlDbContext context,
        ICorrelationContextAccessor correlation,
        ICurrentUserAccessor currentUser,
        ILogger<BusinessEventStore> logger)
    {
        _context = context;
        _correlation = correlation;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Guid> StoreAsync<TEvent>(
        TEvent @event,
        string? justification,
        CancellationToken cancellationToken)
        where TEvent : BusinessEvent
    {
        var correlation = _correlation.Context
            ?? throw new InvalidOperationException("Correlation context required");

        var user = _currentUser.User;

        var storedEvent = new StoredBusinessEvent
        {
            EventId = Guid.NewGuid(),

            // Correlation
            BusinessProcessId = correlation.BusinessProcessId,
            RequestCorrelationId = correlation.RequestCorrelationId,
            SessionCorrelationId = correlation.SessionCorrelationId,
            WorkstreamId = correlation.WorkstreamId,

            // Event identity
            EventType = @event.EventType,
            EventCategory = @event.EventCategory,
            EventVersion = @event.EventVersion,

            // Actor
            ActorId = user.Id,
            ActorType = user.Type.ToString(),
            ActorDisplayName = user.DisplayName,
            ActorIpAddress = user.IpAddress,

            // Temporal
            OccurredAt = @event.OccurredAt,
            RecordedAt = DateTimeOffset.UtcNow,

            // Payload
            EventData = JsonSerializer.Serialize(@event, @event.GetType()),
            Justification = justification,
            AffectedEntities = JsonSerializer.Serialize(@event.AffectedEntities)
        };

        _context.BusinessEvents.Add(storedEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Business event stored: {EventType} [{EventId}] for process {ProcessId}",
            @event.EventType, storedEvent.EventId, correlation.BusinessProcessId);

        return storedEvent.EventId;
    }

    public async Task<IReadOnlyList<Guid>> StoreManyAsync(
        IEnumerable<BusinessEvent> events,
        CancellationToken cancellationToken)
    {
        var eventIds = new List<Guid>();

        foreach (var @event in events)
        {
            var eventId = await StoreAsync(@event, null, cancellationToken);
            eventIds.Add(eventId);
        }

        return eventIds;
    }

    public async Task<IReadOnlyList<BusinessEventSummary>> GetProcessEventsAsync(
        string businessProcessId,
        CancellationToken cancellationToken)
    {
        var events = await _context.BusinessEvents
            .Where(e => e.BusinessProcessId == businessProcessId)
            .OrderBy(e => e.SequenceNumber)
            .Select(e => new BusinessEventSummary(
                e.EventId,
                e.EventType,
                e.EventCategory,
                e.ActorDisplayName ?? e.ActorId,
                e.OccurredAt,
                e.WorkstreamId,
                e.BusinessProcessId))
            .ToListAsync(cancellationToken);

        return events;
    }

    public async Task<PagedResult<BusinessEventSummary>> QueryAsync(
        EventQuery query,
        CancellationToken cancellationToken)
    {
        var queryable = _context.BusinessEvents.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.WorkstreamId))
            queryable = queryable.Where(e => e.WorkstreamId == query.WorkstreamId);

        if (!string.IsNullOrWhiteSpace(query.BusinessProcessId))
            queryable = queryable.Where(e => e.BusinessProcessId == query.BusinessProcessId);

        if (!string.IsNullOrWhiteSpace(query.EventType))
            queryable = queryable.Where(e => e.EventType == query.EventType);

        if (!string.IsNullOrWhiteSpace(query.EventCategory))
            queryable = queryable.Where(e => e.EventCategory == query.EventCategory);

        if (!string.IsNullOrWhiteSpace(query.ActorId))
            queryable = queryable.Where(e => e.ActorId == query.ActorId);

        if (query.FromDate.HasValue)
            queryable = queryable.Where(e => e.OccurredAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            queryable = queryable.Where(e => e.OccurredAt <= query.ToDate.Value);

        // Entity filtering (requires JSON search - simplified for now)
        // In production, consider adding computed columns for common entity searches

        var totalCount = await queryable.CountAsync(cancellationToken);

        // Apply sorting
        queryable = query.SortDescending
            ? queryable.OrderByDescending(e => EF.Property<object>(e, query.SortBy))
            : queryable.OrderBy(e => EF.Property<object>(e, query.SortBy));

        // Apply paging
        var items = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new BusinessEventSummary(
                e.EventId,
                e.EventType,
                e.EventCategory,
                e.ActorDisplayName ?? e.ActorId,
                e.OccurredAt,
                e.WorkstreamId,
                e.BusinessProcessId))
            .ToListAsync(cancellationToken);

        return new PagedResult<BusinessEventSummary>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}
