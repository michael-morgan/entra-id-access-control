using Api.Modules.AccessControl.Models;

namespace Api.Modules.DemoApi.Events.Loans;

/// <summary>
/// Business event: Loan application submitted.
/// </summary>
public record LoanApplicationSubmitted : BusinessEvent
{
    public override string EventCategory => "Loan";

    public Guid LoanId { get; init; }
    public required string ApplicantId { get; init; }
    public required string ApplicantName { get; init; }
    public decimal RequestedAmount { get; init; }
    public int TermMonths { get; init; }
    public required string Region { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Loan", LoanId.ToString())
    };
}

/// <summary>
/// Business event: Loan application approved.
/// </summary>
public record LoanApplicationApproved : BusinessEvent
{
    public override string EventCategory => "Loan";

    public Guid LoanId { get; init; }
    public decimal ApprovedAmount { get; init; }
    public decimal InterestRate { get; init; }
    public bool IsFinalApproval { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Loan", LoanId.ToString())
    };
}

/// <summary>
/// Business event: Loan application rejected.
/// </summary>
public record LoanApplicationRejected : BusinessEvent
{
    public override string EventCategory => "Loan";

    public Guid LoanId { get; init; }
    public required string RejectionReason { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Loan", LoanId.ToString())
    };
}

/// <summary>
/// Business event: Loan status changed.
/// </summary>
public record LoanStatusChanged : BusinessEvent
{
    public override string EventCategory => "Loan";

    public Guid LoanId { get; init; }
    public required string PreviousStatus { get; init; }
    public required string NewStatus { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Loan", LoanId.ToString())
    };
}

/// <summary>
/// Business event: Loan disbursed to applicant.
/// </summary>
public record LoanDisbursed : BusinessEvent
{
    public override string EventCategory => "Loan";

    public Guid LoanId { get; init; }
    public decimal DisbursedAmount { get; init; }
    public required string DisbursementMethod { get; init; }
    public required string TransactionReference { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Loan", LoanId.ToString())
    };
}
