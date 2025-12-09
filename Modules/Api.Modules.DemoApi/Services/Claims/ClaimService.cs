using Api.Modules.DemoApi.Data;
using Api.Modules.DemoApi.Events.Claims;
using Api.Modules.DemoApi.Models.Claims;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;

namespace Api.Modules.DemoApi.Services.Claims;

/// <summary>
/// Business logic for claim operations.
/// ABAC Rules:
/// - High-value claims (>$50K) require ManagementLevel >= 2
/// - Sensitive claims require internal network access
/// - Regional access control
/// </summary>
public interface IClaimService
{
    Task<ClaimDto?> GetClaimAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClaimDto>> GetClaimsAsync(CancellationToken cancellationToken = default);
    Task<ClaimDto> CreateClaimAsync(CreateClaimRequest request, CancellationToken cancellationToken = default);
    Task<ClaimDto> AssignClaimAsync(Guid id, AssignClaimRequest request, CancellationToken cancellationToken = default);
    Task<ClaimDto> AdjudicateClaimAsync(Guid id, AdjudicateClaimRequest request, CancellationToken cancellationToken = default);
    Task<ClaimDto> IssuePaymentAsync(Guid id, string paymentMethod, CancellationToken cancellationToken = default);
}

public class ClaimService(
    IClaimRepository repository,
    IAuthorizationEnforcer enforcer,
    IBusinessEventPublisher eventPublisher,
    IBusinessProcessManager processManager,
    ICorrelationContextAccessor correlationAccessor,
    ICurrentUserAccessor currentUser,
    IUserAttributeStore userAttributeStore) : IClaimService
{
    private readonly IClaimRepository _repository = repository;
    private readonly IAuthorizationEnforcer _enforcer = enforcer;
    private readonly IBusinessEventPublisher _eventPublisher = eventPublisher;
    private readonly IBusinessProcessManager _processManager = processManager;
    private readonly ICorrelationContextAccessor _correlationAccessor = correlationAccessor;
    private readonly ICurrentUserAccessor _currentUser = currentUser;
    private readonly IUserAttributeStore _userAttributeStore = userAttributeStore;

    public async Task<ClaimDto?> GetClaimAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var claim = await _repository.GetByIdAsync(id, cancellationToken);
        if (claim == null)
            return null;

        // Authorization check with entity data (checks sensitive claim rules)
        await _enforcer.EnsureAuthorizedAsync($"Claim/{id}", "read", claim);

        return ClaimDto.FromEntity(claim);
    }

    public async Task<IReadOnlyList<ClaimDto>> GetClaimsAsync(CancellationToken cancellationToken = default)
    {
        // Coarse-grained check
        await _enforcer.EnsureAuthorizedAsync("Claim", "list", null);

        var claims = await _repository.GetAllAsync(cancellationToken);

        // Fine-grained filtering based on user attributes
        var userId = _currentUser.User.Id;
        var workstreamId = "claims";
        var userAttributes = await _userAttributeStore.GetAttributesAsync(userId, workstreamId, cancellationToken);

        // Extract Region from dynamic attributes
        string? userRegion = null;
        if (userAttributes?.Attributes.TryGetValue("Region", out var regionElement) == true)
        {
            userRegion = regionElement.ToString();
        }

        var filteredClaims = claims.Where(claim =>
        {
            // Regional access control
            if (userRegion != null && claim.Region != userRegion)
                return false;

            return true;
        }).ToList();

        return filteredClaims.Select(ClaimDto.FromEntity).ToList();
    }

    public async Task<ClaimDto> CreateClaimAsync(CreateClaimRequest request, CancellationToken cancellationToken = default)
    {
        // Authorization check
        await _enforcer.EnsureAuthorizedAsync("Claim", "create", null);

        var user = _currentUser.User;
        var correlation = _correlationAccessor.Context;

        // Initiate business process if not already set
        string businessProcessId;
        if (string.IsNullOrEmpty(correlation?.BusinessProcessId))
        {
            var process = await _processManager.InitiateProcessAsync(
                "ClaimProcessing",
                "claims",
                new Dictionary<string, object>
                {
                    ["claimantId"] = request.ClaimantId,
                    ["claimAmount"] = request.ClaimAmount,
                    ["claimType"] = request.ClaimType.ToString()
                },
                cancellationToken);
            businessProcessId = process.BusinessProcessId;
        }
        else
        {
            businessProcessId = correlation.BusinessProcessId;
        }

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimantId = request.ClaimantId,
            ClaimantName = request.ClaimantName,
            PolicyNumber = request.PolicyNumber,
            ClaimAmount = request.ClaimAmount,
            ClaimType = request.ClaimType,
            Region = request.Region,
            IsSensitive = request.IsSensitive,
            Status = ClaimStatus.Filed,
            BusinessProcessId = businessProcessId,
            FiledAt = DateTimeOffset.UtcNow
        };

        await _repository.CreateAsync(claim, cancellationToken);

        // Publish business event
        await _eventPublisher.PublishAsync(
            new ClaimFiled
            {
                ClaimId = claim.Id,
                ClaimantId = claim.ClaimantId,
                ClaimantName = claim.ClaimantName,
                PolicyNumber = claim.PolicyNumber,
                ClaimAmount = claim.ClaimAmount,
                ClaimType = claim.ClaimType.ToString(),
                Region = claim.Region,
                IsSensitive = claim.IsSensitive
            },
            justification: $"Claim filed by {user.DisplayName ?? user.Id}",
            cancellationToken);

        return ClaimDto.FromEntity(claim);
    }

    public async Task<ClaimDto> AssignClaimAsync(Guid id, AssignClaimRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Claim {id} not found");

        // Authorization check
        await _enforcer.EnsureAuthorizedAsync($"Claim/{id}", "assign", claim);

        if (claim.Status != ClaimStatus.Filed && claim.Status != ClaimStatus.UnderReview)
            throw new InvalidOperationException($"Claim cannot be assigned in status {claim.Status}");

        claim.AssignedAdjudicatorId = request.AdjudicatorId;
        claim.Status = ClaimStatus.UnderReview;

        await _repository.UpdateAsync(claim, cancellationToken);

        // Publish business event
        await _eventPublisher.PublishAsync(
            new ClaimAssigned
            {
                ClaimId = claim.Id,
                AdjudicatorId = request.AdjudicatorId
            },
            justification: $"Claim assigned to adjudicator {request.AdjudicatorId}",
            cancellationToken);

        return ClaimDto.FromEntity(claim);
    }

    public async Task<ClaimDto> AdjudicateClaimAsync(Guid id, AdjudicateClaimRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Claim {id} not found");

        // Layer 2: Service-level authorization with entity data
        // This checks: management level for high-value claims, region, sensitive claim rules, etc.
        await _enforcer.EnsureAuthorizedAsync($"Claim/{id}", "adjudicate", claim);

        if (claim.Status != ClaimStatus.UnderReview)
            throw new InvalidOperationException($"Claim cannot be adjudicated in status {claim.Status}");

        var previousStatus = claim.Status;
        claim.Status = request.IsApproved ? ClaimStatus.Approved : ClaimStatus.Rejected;
        claim.ApprovedAmount = request.IsApproved ? request.ApprovedAmount : 0;
        claim.AdjudicationNotes = request.AdjudicationNotes;
        claim.AdjudicatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(claim, cancellationToken);

        // Publish business event
        await _eventPublisher.PublishAsync(
            new ClaimAdjudicated
            {
                ClaimId = claim.Id,
                IsApproved = request.IsApproved,
                ApprovedAmount = request.ApprovedAmount
            },
            justification: request.AdjudicationNotes,
            cancellationToken);

        // Publish status change event
        await _eventPublisher.PublishAsync(
            new ClaimStatusChanged
            {
                ClaimId = claim.Id,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = claim.Status.ToString()
            },
            justification: "Status changed due to adjudication",
            cancellationToken);

        // Complete business process
        if (!string.IsNullOrEmpty(claim.BusinessProcessId))
        {
            await _processManager.CompleteProcessAsync(
                claim.BusinessProcessId,
                request.IsApproved ? BusinessProcessOutcome.Approved : BusinessProcessOutcome.Denied,
                request.AdjudicationNotes,
                cancellationToken);
        }

        return ClaimDto.FromEntity(claim);
    }

    public async Task<ClaimDto> IssuePaymentAsync(Guid id, string paymentMethod, CancellationToken cancellationToken = default)
    {
        var claim = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Claim {id} not found");

        // Authorization check
        await _enforcer.EnsureAuthorizedAsync($"Claim/{id}", "pay", claim);

        if (claim.Status != ClaimStatus.Approved)
            throw new InvalidOperationException("Claim must be approved before payment");

        if (!claim.ApprovedAmount.HasValue || claim.ApprovedAmount.Value <= 0)
            throw new InvalidOperationException("Claim has no approved amount");

        var previousStatus = claim.Status;
        claim.Status = ClaimStatus.Paid;

        await _repository.UpdateAsync(claim, cancellationToken);

        // Publish business event
        var transactionRef = $"PAY-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        await _eventPublisher.PublishAsync(
            new ClaimPaymentIssued
            {
                ClaimId = claim.Id,
                PaymentAmount = claim.ApprovedAmount.Value,
                PaymentMethod = paymentMethod,
                TransactionReference = transactionRef
            },
            justification: $"Payment issued via {paymentMethod}",
            cancellationToken);

        // Publish status change event
        await _eventPublisher.PublishAsync(
            new ClaimStatusChanged
            {
                ClaimId = claim.Id,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = claim.Status.ToString()
            },
            justification: "Status changed due to payment",
            cancellationToken);

        return ClaimDto.FromEntity(claim);
    }
}
