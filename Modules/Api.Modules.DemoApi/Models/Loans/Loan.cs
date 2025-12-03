namespace Api.Modules.DemoApi.Models.Loans;

/// <summary>
/// Loan application entity.
/// </summary>
public class Loan
{
    public Guid Id { get; set; }
    public required string ApplicantId { get; set; }
    public required string ApplicantName { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal? InterestRate { get; set; }
    public LoanStatus Status { get; set; }
    public required string Region { get; set; }
    public string? OwnerId { get; set; }
    public string? BusinessProcessId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public enum LoanStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Disbursed,
    Closed
}
