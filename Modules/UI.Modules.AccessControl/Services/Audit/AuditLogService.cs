using UI.Modules.AccessControl.Services.Graph;
using Api.Modules.AccessControl.Persistence.Entities.Audit;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;

namespace UI.Modules.AccessControl.Services.Audit;

/// <summary>
/// Service implementation for managing audit logs with business logic.
/// Orchestrates repository calls and Graph API user lookups.
/// </summary>
public class AuditLogService(
    IAuditLogRepository auditLogRepository,
    CachedGraphUserService cachedGraphUserService,
    ILogger<AuditLogService> logger) : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository = auditLogRepository;
    private readonly CachedGraphUserService _cachedGraphUserService = cachedGraphUserService;
    private readonly ILogger<AuditLogService> _logger = logger;

    public async Task<(IEnumerable<AuditLog> AuditLogs, Dictionary<string, string> UserDisplayNames)>
        GetAuditLogsWithDisplayNamesAsync(
            string? userId = null,
            string? entityType = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageSize = 50)
    {
        var auditLogs = await _auditLogRepository.SearchAsync(
            userId,
            entityType,
            startDate,
            endDate,
            pageSize);

        // Batch fetch user display names from Graph API
        var userIds = auditLogs
            .Select(a => a.UserId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .Cast<string>() // Cast to non-nullable string after filtering
            .ToList();

        Dictionary<string, string> userDisplayNames = [];

        if (userIds.Count > 0)
        {
            try
            {
                var users = await _cachedGraphUserService.GetUsersByIdsAsync(userIds);
                userDisplayNames = users.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.DisplayName ?? kvp.Value.UserPrincipalName ?? kvp.Key
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch user display names from Graph API for audit logs");
            }
        }

        return (auditLogs, userDisplayNames);
    }
}
