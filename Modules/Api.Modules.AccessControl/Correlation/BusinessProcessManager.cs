using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Api.Modules.AccessControl.Persistence.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Correlation;

/// <summary>
/// Manages business process lifecycle.
/// </summary>
public class BusinessProcessManager(
    AccessControlDbContext context,
    ICurrentUserAccessor currentUser,
    ILogger<BusinessProcessManager> logger) : IBusinessProcessManager
{
    private readonly AccessControlDbContext _context = context;
    private readonly ICurrentUserAccessor _currentUser = currentUser;
    private readonly ILogger<BusinessProcessManager> _logger = logger;

    public async Task<BusinessProcess> InitiateProcessAsync(
        string processType,
        string workstreamId,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var user = _currentUser.User;

        var businessProcessId = GenerateBusinessProcessId(processType);

        var entity = new BusinessProcessEntity
        {
            BusinessProcessId = businessProcessId,
            ProcessType = processType,
            WorkstreamId = workstreamId,
            Status = BusinessProcessStatus.Active.ToString(),
            InitiatedBy = user.Id,
            InitiatedAt = DateTimeOffset.UtcNow,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null
        };

        _context.BusinessProcesses.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Business process initiated: {ProcessId} of type {ProcessType}",
            businessProcessId, processType);

        return new BusinessProcess
        {
            BusinessProcessId = entity.BusinessProcessId,
            ProcessType = entity.ProcessType,
            WorkstreamId = entity.WorkstreamId,
            Status = Enum.Parse<BusinessProcessStatus>(entity.Status),
            InitiatedBy = entity.InitiatedBy,
            InitiatedAt = entity.InitiatedAt,
            CompletedAt = entity.CompletedAt,
            Outcome = entity.Outcome != null ? Enum.Parse<BusinessProcessOutcome>(entity.Outcome) : null,
            Metadata = metadata != null ? new Dictionary<string, object>(metadata) : null
        };
    }

    public async Task<BusinessProcess?> GetProcessAsync(
        string businessProcessId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BusinessProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.BusinessProcessId == businessProcessId, cancellationToken);

        if (entity == null)
            return null;

        var metadata = !string.IsNullOrWhiteSpace(entity.Metadata)
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Metadata)
            : null;

        return new BusinessProcess
        {
            BusinessProcessId = entity.BusinessProcessId,
            ProcessType = entity.ProcessType,
            WorkstreamId = entity.WorkstreamId,
            Status = Enum.Parse<BusinessProcessStatus>(entity.Status),
            InitiatedBy = entity.InitiatedBy,
            InitiatedAt = entity.InitiatedAt,
            CompletedAt = entity.CompletedAt,
            Outcome = entity.Outcome != null ? Enum.Parse<BusinessProcessOutcome>(entity.Outcome) : null,
            Metadata = metadata
        };
    }

    public async Task UpdateProcessMetadataAsync(
        string businessProcessId,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BusinessProcesses
            .FirstOrDefaultAsync(p => p.BusinessProcessId == businessProcessId, cancellationToken)
            ?? throw new InvalidOperationException($"Business process {businessProcessId} not found");

        entity.Metadata = JsonSerializer.Serialize(metadata);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Business process metadata updated: {ProcessId}",
            businessProcessId);
    }

    public async Task CompleteProcessAsync(
        string businessProcessId,
        BusinessProcessOutcome outcome,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BusinessProcesses
            .FirstOrDefaultAsync(p => p.BusinessProcessId == businessProcessId, cancellationToken)
            ?? throw new InvalidOperationException($"Business process {businessProcessId} not found");

        entity.Status = BusinessProcessStatus.Completed.ToString();
        entity.Outcome = outcome.ToString();
        entity.CompletedAt = DateTimeOffset.UtcNow;

        // Optionally store notes in metadata
        if (!string.IsNullOrWhiteSpace(notes))
        {
            var metadata = !string.IsNullOrWhiteSpace(entity.Metadata)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Metadata)
                : [];

            if (metadata != null)
            {
                metadata["completionNotes"] = notes;
                entity.Metadata = JsonSerializer.Serialize(metadata);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Business process completed: {ProcessId} with outcome {Outcome}",
            businessProcessId, outcome);
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

    private static string GenerateBusinessProcessId(string processType)
    {
        // Format: ProcessType-YYYYMMDD-NNNNN
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var sequence = Guid.NewGuid().ToString("N")[..5].ToUpper();
        return $"{processType}-{date}-{sequence}";
    }
}
