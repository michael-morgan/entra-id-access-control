using Api.Modules.DemoApi.Data;
using Api.Modules.DemoApi.Events.Loans;
using Api.Modules.DemoApi.Models.Loans;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;

namespace Api.Modules.DemoApi.Services.Loans;

/// <summary>
/// Business logic for loan operations with authorization and event publishing.
/// </summary>
public interface ILoanService
{
    Task<LoanDto?> GetLoanAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoanDto>> GetLoansAsync(CancellationToken cancellationToken = default);
    Task<LoanDto> CreateLoanAsync(CreateLoanRequest request, CancellationToken cancellationToken = default);
    Task<LoanDto> ApproveLoanAsync(Guid id, ApproveLoanRequest request, CancellationToken cancellationToken = default);
    Task<LoanDto> RejectLoanAsync(Guid id, RejectLoanRequest request, CancellationToken cancellationToken = default);
    Task<LoanDto> DisburseLoanAsync(Guid id, string disbursementMethod, CancellationToken cancellationToken = default);
}

public class LoanService : ILoanService
{
    private readonly ILoanRepository _repository;
    private readonly IAuthorizationEnforcer _enforcer;
    private readonly IBusinessEventPublisher _eventPublisher;
    private readonly IBusinessProcessManager _processManager;
    private readonly ICorrelationContextAccessor _correlationAccessor;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserAttributeStore _userAttributeStore;

    public LoanService(
        ILoanRepository repository,
        IAuthorizationEnforcer enforcer,
        IBusinessEventPublisher eventPublisher,
        IBusinessProcessManager processManager,
        ICorrelationContextAccessor correlationAccessor,
        ICurrentUserAccessor currentUser,
        IUserAttributeStore userAttributeStore)
    {
        _repository = repository;
        _enforcer = enforcer;
        _eventPublisher = eventPublisher;
        _processManager = processManager;
        _correlationAccessor = correlationAccessor;
        _currentUser = currentUser;
        _userAttributeStore = userAttributeStore;
    }

    public async Task<LoanDto?> GetLoanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await _repository.GetByIdAsync(id, cancellationToken);
        if (loan == null)
            return null;

        // Authorization check with entity data
        await _enforcer.EnsureAuthorizedAsync($"Loan/{id}", "read", loan);

        return LoanDto.FromEntity(loan);
    }

    public async Task<IReadOnlyList<LoanDto>> GetLoansAsync(CancellationToken cancellationToken = default)
    {
        // Coarse-grained check
        await _enforcer.EnsureAuthorizedAsync("Loan", "list", null);

        var loans = await _repository.GetAllAsync(cancellationToken);

        // Fine-grained filtering based on user attributes
        var userId = _currentUser.User.Id;
        var workstreamId = "loans";
        var userAttributes = await _userAttributeStore.GetAttributesAsync(userId, workstreamId);

        // Extract Region from dynamic attributes
        string? userRegion = null;
        if (userAttributes?.Attributes.TryGetValue("Region", out var regionElement) == true)
        {
            userRegion = regionElement.ToString();
        }

        var filteredLoans = loans.Where(loan =>
        {
            // Users can only see loans in their region (unless they're admins)
            if (userRegion != null && loan.Region != userRegion)
                return false;

            return true;
        }).ToList();

        return filteredLoans.Select(LoanDto.FromEntity).ToList();
    }

    public async Task<LoanDto> CreateLoanAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        // Authorization check
        await _enforcer.EnsureAuthorizedAsync("Loan", "create", null);

        var user = _currentUser.User;
        var correlation = _correlationAccessor.Context;

        // Initiate business process if not already set
        string businessProcessId;
        if (string.IsNullOrEmpty(correlation?.BusinessProcessId))
        {
            var process = await _processManager.InitiateProcessAsync(
                "LoanApplication",
                "loans",
                new Dictionary<string, object>
                {
                    ["applicantId"] = request.ApplicantId,
                    ["requestedAmount"] = request.RequestedAmount
                },
                cancellationToken);
            businessProcessId = process.BusinessProcessId;

            // Update correlation context with the new business process ID
            if (correlation != null)
            {
                _correlationAccessor.Context = correlation with { BusinessProcessId = businessProcessId };
            }
        }
        else
        {
            businessProcessId = correlation.BusinessProcessId;
        }

        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            ApplicantId = request.ApplicantId,
            ApplicantName = request.ApplicantName,
            RequestedAmount = request.RequestedAmount,
            TermMonths = request.TermMonths,
            Region = request.Region,
            Status = LoanStatus.Submitted,
            OwnerId = user.Id,
            BusinessProcessId = businessProcessId,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        await _repository.CreateAsync(loan, cancellationToken);

        // Publish business event
        await _eventPublisher.PublishAsync(
            new LoanApplicationSubmitted
            {
                LoanId = loan.Id,
                ApplicantId = loan.ApplicantId,
                ApplicantName = loan.ApplicantName,
                RequestedAmount = loan.RequestedAmount,
                TermMonths = loan.TermMonths,
                Region = loan.Region
            },
            justification: $"Loan application submitted by {user.DisplayName ?? user.Id}",
            cancellationToken);

        return LoanDto.FromEntity(loan);
    }

    public async Task<LoanDto> ApproveLoanAsync(Guid id, ApproveLoanRequest request, CancellationToken cancellationToken = default)
    {
        var loan = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Loan {id} not found");

        // Layer 2: Service-level authorization with entity data
        // This checks: approval limit, region, status, etc. via ABAC
        await _enforcer.EnsureAuthorizedAsync($"Loan/{id}", "approve", loan);

        if (loan.Status != LoanStatus.Submitted && loan.Status != LoanStatus.UnderReview)
            throw new InvalidOperationException($"Loan cannot be approved in status {loan.Status}");

        var previousStatus = loan.Status;
        loan.Status = LoanStatus.Approved;
        loan.ApprovedAmount = request.ApprovedAmount;
        loan.InterestRate = request.InterestRate;
        loan.ApprovalNotes = request.ApprovalNotes;
        loan.ApprovedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(loan, cancellationToken);

        // Publish business event with justification
        await _eventPublisher.PublishAsync(
            new LoanApplicationApproved
            {
                LoanId = loan.Id,
                ApprovedAmount = request.ApprovedAmount,
                InterestRate = request.InterestRate,
                IsFinalApproval = request.IsFinalApproval
            },
            justification: request.ApprovalNotes ?? "Loan approved",
            cancellationToken);

        // Publish status change event
        await _eventPublisher.PublishAsync(
            new LoanStatusChanged
            {
                LoanId = loan.Id,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = loan.Status.ToString()
            },
            justification: "Status changed due to approval",
            cancellationToken);

        // Complete business process if final approval
        if (request.IsFinalApproval && !string.IsNullOrEmpty(loan.BusinessProcessId))
        {
            await _processManager.CompleteProcessAsync(
                loan.BusinessProcessId,
                BusinessProcessOutcome.Approved,
                request.ApprovalNotes,
                cancellationToken);
        }

        return LoanDto.FromEntity(loan);
    }

    public async Task<LoanDto> RejectLoanAsync(Guid id, RejectLoanRequest request, CancellationToken cancellationToken = default)
    {
        var loan = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Loan {id} not found");

        // Authorization check
        await _enforcer.EnsureAuthorizedAsync($"Loan/{id}", "reject", loan);

        if (loan.Status != LoanStatus.Submitted && loan.Status != LoanStatus.UnderReview)
            throw new InvalidOperationException($"Loan cannot be rejected in status {loan.Status}");

        var previousStatus = loan.Status;
        loan.Status = LoanStatus.Rejected;
        loan.ApprovalNotes = request.RejectionReason;

        await _repository.UpdateAsync(loan, cancellationToken);

        // Publish business event
        await _eventPublisher.PublishAsync(
            new LoanApplicationRejected
            {
                LoanId = loan.Id,
                RejectionReason = request.RejectionReason
            },
            justification: request.RejectionReason,
            cancellationToken);

        // Publish status change event
        await _eventPublisher.PublishAsync(
            new LoanStatusChanged
            {
                LoanId = loan.Id,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = loan.Status.ToString()
            },
            justification: "Status changed due to rejection",
            cancellationToken);

        // Complete business process with rejection outcome
        if (!string.IsNullOrEmpty(loan.BusinessProcessId))
        {
            await _processManager.CompleteProcessAsync(
                loan.BusinessProcessId,
                BusinessProcessOutcome.Denied,
                request.RejectionReason,
                cancellationToken);
        }

        return LoanDto.FromEntity(loan);
    }

    public async Task<LoanDto> DisburseLoanAsync(Guid id, string disbursementMethod, CancellationToken cancellationToken = default)
    {
        var loan = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Loan {id} not found");

        // Authorization check
        await _enforcer.EnsureAuthorizedAsync($"Loan/{id}", "disburse", loan);

        if (loan.Status != LoanStatus.Approved)
            throw new InvalidOperationException($"Loan must be approved before disbursement");

        if (!loan.ApprovedAmount.HasValue)
            throw new InvalidOperationException("Loan has no approved amount");

        var previousStatus = loan.Status;
        loan.Status = LoanStatus.Disbursed;

        await _repository.UpdateAsync(loan, cancellationToken);

        // Publish business event
        var transactionRef = $"TXN-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        await _eventPublisher.PublishAsync(
            new LoanDisbursed
            {
                LoanId = loan.Id,
                DisbursedAmount = loan.ApprovedAmount.Value,
                DisbursementMethod = disbursementMethod,
                TransactionReference = transactionRef
            },
            justification: $"Loan disbursed via {disbursementMethod}",
            cancellationToken);

        // Publish status change event
        await _eventPublisher.PublishAsync(
            new LoanStatusChanged
            {
                LoanId = loan.Id,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = loan.Status.ToString()
            },
            justification: "Status changed due to disbursement",
            cancellationToken);

        return LoanDto.FromEntity(loan);
    }
}
