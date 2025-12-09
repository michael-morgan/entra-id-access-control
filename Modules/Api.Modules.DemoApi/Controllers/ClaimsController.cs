using Api.Modules.DemoApi.Models.Claims;
using Api.Modules.DemoApi.Services.Claims;
using Api.Modules.AccessControl.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.DemoApi.Controllers;

/// <summary>
/// Claims workstream controller.
/// ABAC Rules enforced:
/// - High-value claims (>$50K) require ManagementLevel >= 2 for adjudication
/// - Sensitive claims require internal network access
/// - Regional access control
/// </summary>
[ApiController]
[Route("api/claims")]
[Authorize]
public class ClaimsController(
    IClaimService claimService,
    ILogger<ClaimsController> logger) : ControllerBase
{
    private readonly IClaimService _claimService = claimService;
    private readonly ILogger<ClaimsController> _logger = logger;

    /// <summary>
    /// Get all claims (filtered by user's region).
    /// </summary>
    [HttpGet]
    [AuthorizeResource("Claim", "list")]
    public async Task<ActionResult<IEnumerable<ClaimDto>>> GetClaims(CancellationToken cancellationToken)
    {
        var claims = await _claimService.GetClaimsAsync(cancellationToken);
        return Ok(claims);
    }

    /// <summary>
    /// Get a specific claim by ID.
    /// </summary>
    [HttpGet("{id}")]
    [AuthorizeResource("Claim/:id", "read")]
    public async Task<ActionResult<ClaimDto>> GetClaim(Guid id, CancellationToken cancellationToken)
    {
        var claim = await _claimService.GetClaimAsync(id, cancellationToken);

        if (claim == null)
            return NotFound(new { error = $"Claim {id} not found" });

        return Ok(claim);
    }

    /// <summary>
    /// File a new insurance claim.
    /// </summary>
    [HttpPost]
    [AuthorizeResource("Claim", "create")]
    public async Task<ActionResult<ClaimDto>> CreateClaim(
        [FromBody] CreateClaimRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimService.CreateClaimAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetClaim), new { id = claim.Id }, claim);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized claim creation attempt");
            return Forbid();
        }
    }

    /// <summary>
    /// Assign a claim to an adjudicator.
    /// </summary>
    [HttpPost("{id}/assign")]
    [AuthorizeResource("Claim/:id", "assign")]
    public async Task<ActionResult<ClaimDto>> AssignClaim(
        Guid id,
        [FromBody] AssignClaimRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimService.AssignClaimAsync(id, request, cancellationToken);
            return Ok(claim);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid claim assignment attempt for claim {ClaimId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized claim assignment attempt for claim {ClaimId}", id);
            return Forbid();
        }
    }

    /// <summary>
    /// Adjudicate a claim (approve or reject).
    /// ABAC Rules:
    /// - High-value claims (>$50K) require ManagementLevel >= 2
    /// - Sensitive claims require internal network access
    /// </summary>
    [HttpPost("{id}/adjudicate")]
    [AuthorizeResource("Claim/:id", "adjudicate")]
    public async Task<ActionResult<ClaimDto>> AdjudicateClaim(
        Guid id,
        [FromBody] AdjudicateClaimRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var claim = await _claimService.AdjudicateClaimAsync(id, request, cancellationToken);
            return Ok(claim);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid claim adjudication attempt for claim {ClaimId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized claim adjudication attempt for claim {ClaimId}", id);
            return Forbid();
        }
    }

    /// <summary>
    /// Issue payment for an approved claim.
    /// </summary>
    [HttpPost("{id}/pay")]
    [AuthorizeResource("Claim/:id", "pay")]
    public async Task<ActionResult<ClaimDto>> IssuePayment(
        Guid id,
        [FromQuery] string paymentMethod = "DirectDeposit",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var claim = await _claimService.IssuePaymentAsync(id, paymentMethod, cancellationToken);
            return Ok(claim);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid payment issuance attempt for claim {ClaimId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized payment issuance attempt for claim {ClaimId}", id);
            return Forbid();
        }
    }
}
