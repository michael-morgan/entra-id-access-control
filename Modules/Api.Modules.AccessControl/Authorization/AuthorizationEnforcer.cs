using System.Security.Claims;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Casbin;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Service-layer authorization enforcer using Casbin + ABAC.
/// </summary>
public class AuthorizationEnforcer(
    IEnforcer casbinEnforcer,
    IAbacContextProvider abacContextProvider,
    IHttpContextAccessor httpContextAccessor,
    ICorrelationContextAccessor correlationContextAccessor,
    ILogger<AuthorizationEnforcer> logger) : IAuthorizationEnforcer
{
    private readonly IEnforcer _casbinEnforcer = casbinEnforcer;
    private readonly IAbacContextProvider _abacContextProvider = abacContextProvider;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ICorrelationContextAccessor _correlationContextAccessor = correlationContextAccessor;
    private readonly ILogger<AuthorizationEnforcer> _logger = logger;

    public async Task<AuthorizationResult> CheckAsync(
        string resource,
        string action,
        object? resourceEntity = null)
    {
        var user = _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("User context not available");

        var workstreamId = _correlationContextAccessor.Context?.WorkstreamId
            ?? throw new InvalidOperationException("Workstream context not available");

        return await CheckInternalAsync(user, workstreamId, resource, action, resourceEntity);
    }

    public async Task<AuthorizationResult> CheckAsync<TEntity>(TEntity entity, string action)
        where TEntity : class
    {
        var resourceName = typeof(TEntity).Name;
        return await CheckAsync(resourceName, action, entity);
    }

    public async Task EnsureAuthorizedAsync(
        string resource,
        string action,
        object? resourceEntity = null)
    {
        var result = await CheckAsync(resource, action, resourceEntity);

        if (!result.IsAllowed)
        {
            _logger.LogWarning(
                "Authorization denied: Resource={Resource}, Action={Action}, Reason={Reason}",
                resource, action, result.DenialReason);

            throw new UnauthorizedAccessException(
                result.DenialReason ?? $"Access denied to {action} on {resource}");
        }
    }

    public async Task EnsureAuthorizedAsync<TEntity>(TEntity entity, string action)
        where TEntity : class
    {
        var resourceName = typeof(TEntity).Name;
        await EnsureAuthorizedAsync(resourceName, action, entity);
    }

    private async Task<AuthorizationResult> CheckInternalAsync(
        ClaimsPrincipal user,
        string workstreamId,
        string resource,
        string action,
        object? resourceEntity)
    {
        try
        {
            // Build ABAC context
            var abacContext = await _abacContextProvider.BuildContextAsync(
                user,
                workstreamId,
                resource,
                action,
                resourceEntity);

            var contextJson = abacContext.ToJson();

            // Get subject (user ID) and groups from JWT token
            var oidClaim = user.FindFirst("oid")?.Value;
            var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var subject = oidClaim ?? subClaim
                ?? throw new InvalidOperationException("User ID not found");

            // Extract all group IDs from JWT token
            var groupClaims = user.FindAll("groups").Select(c => c.Value).ToList();

            Console.WriteLine($"[CASBIN DEBUG] oid claim: {oidClaim}, sub claim: {subClaim}");
            Console.WriteLine($"[CASBIN DEBUG] User subject: {subject}");
            Console.WriteLine($"[CASBIN DEBUG] User groups: {string.Join(", ", groupClaims)}");
            Console.WriteLine($"[CASBIN DEBUG] Enforce params: workstream={workstreamId}, res={resource}, act={action}");

            // Try authorization with user ID first (fallback for legacy policies)
            var allowed = _casbinEnforcer.Enforce(
                subject,
                workstreamId,
                resource,
                action,
                contextJson);

            Console.WriteLine($"[CASBIN DEBUG] User-based authorization: {allowed}");

            // Debug: Check if roles exist for the group
            foreach (var groupId in groupClaims)
            {
                var rolesForGroup = _casbinEnforcer.GetRolesForUser(groupId, workstreamId);
                Console.WriteLine($"[CASBIN DEBUG] Roles for group {groupId}: {string.Join(", ", rolesForGroup)}");
            }

            // If user-based check fails, try group-based authorization
            if (!allowed && groupClaims.Count != 0)
            {
                Console.WriteLine($"[CASBIN DEBUG] Checking {groupClaims.Count} groups for authorization...");

                foreach (var groupId in groupClaims)
                {
                    Console.WriteLine($"[CASBIN DEBUG] Checking group: {groupId}");

                    var groupAllowed = _casbinEnforcer.Enforce(
                        groupId,
                        workstreamId,
                        resource,
                        action,
                        contextJson);

                    Console.WriteLine($"[CASBIN DEBUG] Group '{groupId}' authorization: {groupAllowed}");

                    if (groupAllowed)
                    {
                        allowed = true;
                        Console.WriteLine($"[CASBIN DEBUG] Authorization GRANTED via group: {groupId}");
                        break;
                    }
                }
            }

            Console.WriteLine($"[CASBIN DEBUG] Final authorization result: {allowed}");

            if (!allowed)
            {
                return new AuthorizationResult(
                    false,
                    $"User '{subject}' is not authorized to '{action}' on '{resource}' in workstream '{workstreamId}'");
            }

            _logger.LogDebug(
                "Authorization granted: User={User}, Workstream={Workstream}, Resource={Resource}, Action={Action}",
                subject, workstreamId, resource, action);

            return new AuthorizationResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Authorization check failed: Resource={Resource}, Action={Action}",
                resource, action);

            return new AuthorizationResult(false, $"Authorization check failed: {ex.Message}");
        }
    }
}
