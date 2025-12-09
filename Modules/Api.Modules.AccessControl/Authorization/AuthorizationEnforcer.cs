using System.Security.Claims;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Service-layer authorization enforcer using policy engine + ABAC.
/// </summary>
public class AuthorizationEnforcer(
    IPolicyEngine policyEngine,
    IAbacContextProvider abacContextProvider,
    IHttpContextAccessor httpContextAccessor,
    ICorrelationContextAccessor correlationContextAccessor,
    ILogger<AuthorizationEnforcer> logger) : IAuthorizationEnforcer
{
    private readonly IPolicyEngine _policyEngine = policyEngine;
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

            _logger.LogDebug("Starting authorization check for subject: {Subject} with {GroupCount} groups",
                subject, groupClaims.Count);
            _logger.LogDebug("Authorization parameters - Workstream: {Workstream}, Resource: {Resource}, Action: {Action}",
                workstreamId, resource, action);

            // Try authorization with user ID first (fallback for legacy policies)
            var allowed = _policyEngine.Enforce(
                subject,
                workstreamId,
                resource,
                action,
                contextJson);

            _logger.LogDebug("User-based authorization result: {Allowed}", allowed);

            // Debug: Check if roles exist for the group
            foreach (var groupId in groupClaims)
            {
                var rolesForGroup = _policyEngine.GetRolesForSubject(groupId, workstreamId);
                _logger.LogDebug("Group {GroupId} has {RoleCount} roles in workstream {Workstream}",
                    groupId, rolesForGroup.Count(), workstreamId);
            }

            // If user-based check fails, try group-based authorization
            if (!allowed && groupClaims.Count != 0)
            {
                _logger.LogDebug("Checking {GroupCount} groups for authorization", groupClaims.Count);

                foreach (var groupId in groupClaims)
                {
                    _logger.LogDebug("Evaluating authorization for group: {GroupId}", groupId);

                    var groupAllowed = _policyEngine.Enforce(
                        groupId,
                        workstreamId,
                        resource,
                        action,
                        contextJson);

                    _logger.LogDebug("Group {GroupId} authorization result: {Allowed}", groupId, groupAllowed);

                    if (groupAllowed)
                    {
                        allowed = true;
                        _logger.LogInformation("Authorization granted via group: {GroupId}", groupId);
                        break;
                    }
                }
            }

            _logger.LogDebug("Final authorization result: {Allowed}", allowed);

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
