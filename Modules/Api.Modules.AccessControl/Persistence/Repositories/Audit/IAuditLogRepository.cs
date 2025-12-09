using Api.Modules.AccessControl.Persistence.Entities.Audit;

namespace Api.Modules.AccessControl.Persistence.Repositories.Audit;

/// <summary>
/// Repository interface for querying audit logs.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Searches audit logs with optional filtering.
    /// </summary>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="entityType">Optional entity type filter</param>
    /// <param name="startDate">Start date for filtering</param>
    /// <param name="endDate">End date for filtering</param>
    /// <param name="pageSize">Maximum number of records to return</param>
    /// <returns>Collection of audit logs matching the criteria</returns>
    Task<IEnumerable<AuditLog>> SearchAsync(
        string? userId = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageSize = 50);

    /// <summary>
    /// Gets all distinct user IDs from audit logs.
    /// </summary>
    /// <returns>Collection of user IDs</returns>
    Task<IEnumerable<string>> GetDistinctUserIdsAsync();

    /// <summary>
    /// Gets all distinct entity types from audit logs.
    /// </summary>
    /// <returns>Collection of entity types</returns>
    Task<IEnumerable<string>> GetDistinctEntityTypesAsync();
}
