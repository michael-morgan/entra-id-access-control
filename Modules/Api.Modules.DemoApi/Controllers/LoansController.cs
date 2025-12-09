using Api.Modules.DemoApi.Models.Loans;
using Api.Modules.DemoApi.Services.Loans;
using Api.Modules.AccessControl.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.DemoApi.Controllers;

/// <summary>
/// Loans workstream controller demonstrating multi-layer authorization.
/// </summary>
[ApiController]
[Route("api/loans")]
[Authorize]
public class LoansController(
    ILoanService loanService,
    ILogger<LoansController> logger) : ControllerBase
{
    private readonly ILoanService _loanService = loanService;
    private readonly ILogger<LoansController> _logger = logger;

    /// <summary>
    /// Get all loans (filtered by user's region via ABAC).
    /// </summary>
    [HttpGet]
    [AuthorizeResource("Loan", "list")]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetLoans(CancellationToken cancellationToken)
    {
        var loans = await _loanService.GetLoansAsync(cancellationToken);
        return Ok(loans);
    }

    /// <summary>
    /// Get a specific loan by ID.
    /// Layer 1: Coarse-grained check at endpoint.
    /// </summary>
    [HttpGet("{id}")]
    [AuthorizeResource("Loan/:id", "read")]
    public async Task<ActionResult<LoanDto>> GetLoan(Guid id, CancellationToken cancellationToken)
    {
        var loan = await _loanService.GetLoanAsync(id, cancellationToken);

        if (loan == null)
            return NotFound(new { error = $"Loan {id} not found" });

        return Ok(loan);
    }

    /// <summary>
    /// Create a new loan application.
    /// </summary>
    [HttpPost]
    [AuthorizeResource("Loan", "create")]
    public async Task<ActionResult<LoanDto>> CreateLoan(
        [FromBody] CreateLoanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var loan = await _loanService.CreateLoanAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, loan);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized loan creation attempt");
            return Forbid();
        }
    }

    /// <summary>
    /// Approve a loan application.
    /// Layer 1: Coarse-grained check at endpoint.
    /// Layer 2: Fine-grained check in service with entity data (approval limit, region, etc.).
    /// </summary>
    [HttpPost("{id}/approve")]
    [AuthorizeResource("Loan/:id", "approve")]
    public async Task<ActionResult<LoanDto>> ApproveLoan(
        Guid id,
        [FromBody] ApproveLoanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var loan = await _loanService.ApproveLoanAsync(id, request, cancellationToken);
            return Ok(loan);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid loan approval attempt for loan {LoanId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized loan approval attempt for loan {LoanId}", id);
            return Forbid();
        }
    }

    /// <summary>
    /// Reject a loan application.
    /// </summary>
    [HttpPost("{id}/reject")]
    [AuthorizeResource("Loan/:id", "reject")]
    public async Task<ActionResult<LoanDto>> RejectLoan(
        Guid id,
        [FromBody] RejectLoanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var loan = await _loanService.RejectLoanAsync(id, request, cancellationToken);
            return Ok(loan);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid loan rejection attempt for loan {LoanId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized loan rejection attempt for loan {LoanId}", id);
            return Forbid();
        }
    }

    /// <summary>
    /// Disburse an approved loan.
    /// </summary>
    [HttpPost("{id}/disburse")]
    [AuthorizeResource("Loan/:id", "disburse")]
    public async Task<ActionResult<LoanDto>> DisburseLoan(
        Guid id,
        [FromQuery] string disbursementMethod = "BankTransfer",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var loan = await _loanService.DisburseLoanAsync(id, disbursementMethod, cancellationToken);
            return Ok(loan);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid loan disbursement attempt for loan {LoanId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized loan disbursement attempt for loan {LoanId}", id);
            return Forbid();
        }
    }
}
