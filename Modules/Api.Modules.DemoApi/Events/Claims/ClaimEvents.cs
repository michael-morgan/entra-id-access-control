using Api.Modules.AccessControl.Models;

namespace Api.Modules.DemoApi.Events.Claims;

/// <summary>
/// Business event: Claim filed.
/// </summary>
public record ClaimFiled : BusinessEvent
{
    public override string EventCategory => "Claim";

    public Guid ClaimId { get; init; }
    public required string ClaimantId { get; init; }
    public required string ClaimantName { get; init; }
    public required string PolicyNumber { get; init; }
    public decimal ClaimAmount { get; init; }
    public required string ClaimType { get; init; }
    public required string Region { get; init; }
    public bool IsSensitive { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Claim", ClaimId.ToString())
    };
}

/// <summary>
/// Business event: Claim assigned to adjudicator.
/// </summary>
public record ClaimAssigned : BusinessEvent
{
    public override string EventCategory => "Claim";

    public Guid ClaimId { get; init; }
    public required string AdjudicatorId { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Claim", ClaimId.ToString())
    };
}

/// <summary>
/// Business event: Claim adjudicated (approved or rejected).
/// </summary>
public record ClaimAdjudicated : BusinessEvent
{
    public override string EventCategory => "Claim";

    public Guid ClaimId { get; init; }
    public bool IsApproved { get; init; }
    public decimal ApprovedAmount { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Claim", ClaimId.ToString())
    };
}

/// <summary>
/// Business event: Claim status changed.
/// </summary>
public record ClaimStatusChanged : BusinessEvent
{
    public override string EventCategory => "Claim";

    public Guid ClaimId { get; init; }
    public required string PreviousStatus { get; init; }
    public required string NewStatus { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Claim", ClaimId.ToString())
    };
}

/// <summary>
/// Business event: Claim payment issued.
/// </summary>
public record ClaimPaymentIssued : BusinessEvent
{
    public override string EventCategory => "Claim";

    public Guid ClaimId { get; init; }
    public decimal PaymentAmount { get; init; }
    public required string PaymentMethod { get; init; }
    public required string TransactionReference { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Claim", ClaimId.ToString())
    };
}
