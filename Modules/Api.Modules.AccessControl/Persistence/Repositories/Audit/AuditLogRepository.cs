using Api.Modules.AccessControl.Persistence.Entities.Audit;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Audit;

/// <summary>
/// Repository implementation for querying audit logs.
/// </summary>
public class AuditLogRepository(AccessControlDbContext context) : IAuditLogRepository
{
    private readonly AccessControlDbContext _context = context;

    public async Task<IEnumerable<AuditLog>> SearchAsync(
        string? userId = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageSize = 50)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.UpdatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.UpdatedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(a => a.UpdatedAt)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetDistinctUserIdsAsync()
    {
        return await _context.AuditLogs
            .Where(a => a.UserId != null)
            .Select(a => a.UserId!)
            .Distinct()
            .OrderBy(u => u)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetDistinctEntityTypesAsync()
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType != null)
            .Select(a => a.EntityType!)
            .Distinct()
            .OrderBy(t => t)
            .AsNoTracking()
            .ToListAsync();
    }
}
