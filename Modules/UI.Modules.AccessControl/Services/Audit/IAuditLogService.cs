using Api.Modules.AccessControl.Persistence.Entities.Audit;

namespace UI.Modules.AccessControl.Services.Audit;

/// <summary>
/// Service interface for managing audit logs with business logic.
/// Orchestrates repository calls and Graph API user lookups.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Gets audit logs with user display names resolved from Graph API.
    /// </summary>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="entityType">Optional entity type filter</param>
    /// <param name="startDate">Start date for filtering</param>
    /// <param name="endDate">End date for filtering</param>
    /// <param name="pageSize">Maximum number of records to return</param>
    /// <returns>Tuple of (auditLogs, userDisplayNames)</returns>
    Task<(IEnumerable<AuditLog> AuditLogs, Dictionary<string, string> UserDisplayNames)>
        GetAuditLogsWithDisplayNamesAsync(
            string? userId = null,
            string? entityType = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageSize = 50);
}
