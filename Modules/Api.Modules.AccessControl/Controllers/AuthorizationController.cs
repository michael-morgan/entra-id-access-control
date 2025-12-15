using Api.Modules.AccessControl.Correlation;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Controllers;

/// <summary>
/// REST API for authorization checks from external applications.
/// Provides centralized authorization decisions via HTTP endpoints.
/// </summary>
[ApiController]
[Route("api/authorization")]
[Authorize] // Requires valid JWT token
public class AuthorizationController(
    IAuthorizationEnforcer enforcer,
    ICorrelationContextAccessor correlationContextAccessor,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<AuthorizationController> logger) : ControllerBase
{
    private readonly IAuthorizationEnforcer _enforcer = enforcer;
    private readonly ICorrelationContextAccessor _correlationContextAccessor = correlationContextAccessor;
    private readonly ICurrentUserAccessor _currentUserAccessor = currentUserAccessor;
    private readonly ILogger<AuthorizationController> _logger = logger;

    /// <summary>
    /// Check if the authenticated user is authorized to perform an action on a resource.
    /// </summary>
    /// <param name="request">Authorization check request</param>
    /// <returns>Authorization decision</returns>
    /// <response code="200">Returns authorization decision</response>
    /// <response code="400">If request is invalid</response>
    /// <response code="401">If not authenticated</response>
    [HttpPost("check")]
    [ProducesResponseType(typeof(AuthorizationCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckAuthorization([FromBody] AuthorizationCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Resource) || string.IsNullOrWhiteSpace(request.Action))
        {
            return BadRequest(new { error = "Resource and Action are required" });
        }

        // Use workstream from request, header, or default
        var workstreamId = request.WorkstreamId
            ?? _correlationContextAccessor.Context?.WorkstreamId
            ?? "global";

        var userId = _currentUserAccessor.User?.Id ?? "unknown";

        _logger.LogDebug(
            "Authorization check: User={UserId}, Resource={Resource}, Action={Action}, Workstream={WorkstreamId}",
            userId,
            request.Resource,
            request.Action,
            workstreamId
        );

        try
        {
            // Perform authorization check - pass workstreamId explicitly
            var result = await _enforcer.CheckAsync(
                request.Resource,
                request.Action,
                request.EntityData,
                workstreamId
            );

            var response = new AuthorizationCheckResponse
            {
                Resource = request.Resource,
                Action = request.Action,
                Allowed = result.IsAllowed,
                Reason = result.IsAllowed ? null : result.DenialReason,
                WorkstreamId = workstreamId
            };

            _logger.LogInformation(
                "Authorization {Result}: User={UserId}, Resource={Resource}, Action={Action}, Workstream={WorkstreamId}, Reason={Reason}",
                result.IsAllowed ? "ALLOWED" : "DENIED",
                userId,
                request.Resource,
                request.Action,
                workstreamId,
                result.DenialReason
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking authorization: User={UserId}, Resource={Resource}, Action={Action}",
                userId,
                request.Resource,
                request.Action
            );
            return StatusCode(500, new { error = "Internal server error during authorization check" });
        }
    }

    /// <summary>
    /// Check multiple resource/action combinations in a single request (batch check).
    /// More efficient than multiple single checks when loading a page with many authorization decisions.
    /// </summary>
    /// <param name="request">Batch authorization check request</param>
    /// <returns>List of authorization decisions</returns>
    /// <response code="200">Returns list of authorization decisions</response>
    /// <response code="400">If request is invalid</response>
    /// <response code="401">If not authenticated</response>
    [HttpPost("check-batch")]
    [ProducesResponseType(typeof(List<AuthorizationCheckResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckBatchAuthorization([FromBody] BatchAuthorizationCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkstreamId))
        {
            return BadRequest(new { error = "WorkstreamId is required" });
        }

        if (request.Checks == null || request.Checks.Count == 0)
        {
            return BadRequest(new { error = "At least one check is required" });
        }

        var userId = _currentUserAccessor.User?.Id ?? "unknown";

        _logger.LogDebug(
            "Batch authorization check: User={UserId}, Workstream={WorkstreamId}, Count={Count}",
            userId,
            request.WorkstreamId,
            request.Checks.Count
        );

        var responses = new List<AuthorizationCheckResponse>();

        foreach (var check in request.Checks)
        {
            if (string.IsNullOrWhiteSpace(check.Resource) || string.IsNullOrWhiteSpace(check.Action))
            {
                continue; // Skip invalid checks
            }

            try
            {
                var result = await _enforcer.CheckAsync(
                    check.Resource,
                    check.Action,
                    resourceEntity: null, // Batch checks typically don't include entity data
                    workstreamId: request.WorkstreamId
                );

                responses.Add(new AuthorizationCheckResponse
                {
                    Resource = check.Resource,
                    Action = check.Action,
                    Allowed = result.IsAllowed,
                    Reason = result.IsAllowed ? null : result.DenialReason,
                    WorkstreamId = request.WorkstreamId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in batch check: Resource={Resource}, Action={Action}",
                    check.Resource,
                    check.Action
                );

                // Add denial for failed checks
                responses.Add(new AuthorizationCheckResponse
                {
                    Resource = check.Resource,
                    Action = check.Action,
                    Allowed = false,
                    Reason = "Internal error during authorization check",
                    WorkstreamId = request.WorkstreamId
                });
            }
        }

        _logger.LogInformation(
            "Batch authorization complete: User={UserId}, Total={Total}, Allowed={Allowed}, Denied={Denied}",
            userId,
            responses.Count,
            responses.Count(r => r.Allowed),
            responses.Count(r => !r.Allowed)
        );

        return Ok(responses);
    }
}
