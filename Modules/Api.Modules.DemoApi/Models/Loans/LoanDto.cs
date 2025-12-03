namespace Api.Modules.DemoApi.Models.Loans;

/// <summary>
/// Data transfer object for Loan.
/// </summary>
public record LoanDto
{
    public Guid Id { get; init; }
    public string ApplicantId { get; init; } = string.Empty;
    public string ApplicantName { get; init; } = string.Empty;
    public decimal RequestedAmount { get; init; }
    public decimal? ApprovedAmount { get; init; }
    public int TermMonths { get; init; }
    public decimal? InterestRate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string? OwnerId { get; init; }
    public string? BusinessProcessId { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public DateTimeOffset? ApprovedAt { get; init; }
    public string? ApprovalNotes { get; init; }

    public static LoanDto FromEntity(Loan loan) => new()
    {
        Id = loan.Id,
        ApplicantId = loan.ApplicantId,
        ApplicantName = loan.ApplicantName,
        RequestedAmount = loan.RequestedAmount,
        ApprovedAmount = loan.ApprovedAmount,
        TermMonths = loan.TermMonths,
        InterestRate = loan.InterestRate,
        Status = loan.Status.ToString(),
        Region = loan.Region,
        OwnerId = loan.OwnerId,
        BusinessProcessId = loan.BusinessProcessId,
        SubmittedAt = loan.SubmittedAt,
        ApprovedAt = loan.ApprovedAt,
        ApprovalNotes = loan.ApprovalNotes
    };
}

public record CreateLoanRequest
{
    public required string ApplicantId { get; init; }
    public required string ApplicantName { get; init; }
    public decimal RequestedAmount { get; init; }
    public int TermMonths { get; init; }
    public required string Region { get; init; }
}

public record ApproveLoanRequest
{
    public decimal ApprovedAmount { get; init; }
    public decimal InterestRate { get; init; }
    public string? ApprovalNotes { get; init; }
    public bool IsFinalApproval { get; init; }
}

public record RejectLoanRequest
{
    public required string RejectionReason { get; init; }
}
