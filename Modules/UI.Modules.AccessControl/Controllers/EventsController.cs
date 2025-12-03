using Api.Modules.AccessControl.Persistence;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for viewing business events and audit logs.
/// </summary>
public class EventsController : Controller
{
    private readonly IBusinessEventQueryService _eventQueryService;
    private readonly AccessControlDbContext _auditContext;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IBusinessEventQueryService eventQueryService,
        AccessControlDbContext auditContext,
        ILogger<EventsController> logger)
    {
        _eventQueryService = eventQueryService;
        _auditContext = auditContext;
        _logger = logger;
    }

    // GET: Events
    public async Task<IActionResult> Index(
        string? workstreamId = null,
        string? eventType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageSize = 50,
        int pageNumber = 1)
    {
        // Default to last 7 days
        startDate ??= DateTime.UtcNow.AddDays(-7);
        endDate ??= DateTime.UtcNow;

        var query = new EventQuery
        {
            WorkstreamId = workstreamId,
            EventType = eventType,
            FromDate = startDate.Value,
            ToDate = endDate.Value,
            PageSize = pageSize,
            PageNumber = pageNumber
        };

        var result = await _eventQueryService.QueryAsync(query);

        ViewBag.WorkstreamId = workstreamId;
        ViewBag.EventType = eventType;
        ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
        ViewBag.PageSize = pageSize;
        ViewBag.PageNumber = pageNumber;
        ViewBag.TotalPages = result.TotalPages;

        // Get distinct workstreams and event types for filters (from first page of all events)
        var allEventsQuery = new EventQuery
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            PageSize = 100,
            PageNumber = 1
        };
        var allEvents = await _eventQueryService.QueryAsync(allEventsQuery);

        ViewBag.Workstreams = allEvents.Items
            .Select(e => e.WorkstreamId)
            .Distinct()
            .OrderBy(w => w)
            .ToList();

        ViewBag.EventTypes = allEvents.Items
            .Select(e => e.EventType)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        return View(result);
    }

    // GET: Events/Details/guid
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventDetail = await _eventQueryService.GetEventDetailAsync(id.Value);

        if (eventDetail == null)
        {
            return NotFound();
        }

        return View(eventDetail);
    }

    // GET: Events/Audit
    public async Task<IActionResult> Audit(
        string? userId = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageSize = 50)
    {
        // Default to last 7 days
        startDate ??= DateTime.UtcNow.AddDays(-7);
        endDate ??= DateTime.UtcNow;

        var query = _auditContext.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        query = query.Where(a => a.UpdatedAt >= startDate.Value && a.UpdatedAt <= endDate.Value);

        var auditLogs = await query
            .OrderByDescending(a => a.UpdatedAt)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.UserId = userId;
        ViewBag.EntityType = entityType;
        ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
        ViewBag.PageSize = pageSize;

        return View(auditLogs);
    }

    // GET: Events/ProcessTimeline/business-process-id
    public async Task<IActionResult> ProcessTimeline(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var events = await _eventQueryService.GetProcessTimelineAsync(id);

        if (!events.Any())
        {
            return NotFound();
        }

        ViewBag.BusinessProcessId = id;
        return View(events);
    }
}
