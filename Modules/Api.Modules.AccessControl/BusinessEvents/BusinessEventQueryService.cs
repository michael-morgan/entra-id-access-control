using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.BusinessEvents;

/// <summary>
/// Query service for business events.
/// </summary>
public class BusinessEventQueryService : IBusinessEventQueryService
{
    private readonly AccessControlDbContext _context;

    public BusinessEventQueryService(AccessControlDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BusinessEventSummary>> QueryAsync(
        EventQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.BusinessEvents.AsQueryable();

        // Apply filters (same as in BusinessEventStore)
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

        var totalCount = await queryable.CountAsync(cancellationToken);

        queryable = query.SortDescending
            ? queryable.OrderByDescending(e => EF.Property<object>(e, query.SortBy))
            : queryable.OrderBy(e => EF.Property<object>(e, query.SortBy));

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

    public async Task<IReadOnlyList<BusinessEventSummary>> GetProcessTimelineAsync(
        string businessProcessId,
        CancellationToken cancellationToken = default)
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

    public async Task<BusinessEventDetail?> GetEventDetailAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var @event = await _context.BusinessEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);

        if (@event == null)
            return null;

        var affectedEntities = string.IsNullOrWhiteSpace(@event.AffectedEntities)
            ? Array.Empty<AffectedEntity>()
            : JsonSerializer.Deserialize<AffectedEntity[]>(@event.AffectedEntities) ?? Array.Empty<AffectedEntity>();

        return new BusinessEventDetail(
            @event.EventId,
            @event.SequenceNumber,
            @event.EventType,
            @event.EventCategory,
            @event.EventVersion,
            @event.BusinessProcessId,
            @event.WorkstreamId,
            @event.ActorId,
            @event.ActorType,
            @event.ActorDisplayName ?? @event.ActorId,
            @event.OccurredAt,
            @event.RecordedAt,
            @event.EventData,
            @event.Justification,
            affectedEntities);
    }

    public async Task<IReadOnlyList<BusinessEventSummary>> GetEntityEventsAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        // This requires searching JSON - simplified implementation
        // In production, consider adding computed columns or full-text search
        var searchString = $"\"{entityType}:{entityId}\"";

        var events = await _context.BusinessEvents
            .Where(e => e.AffectedEntities != null && e.AffectedEntities.Contains(searchString))
            .OrderByDescending(e => e.SequenceNumber)
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
}
