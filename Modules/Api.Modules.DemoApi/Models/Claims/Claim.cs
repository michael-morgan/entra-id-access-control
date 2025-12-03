namespace Api.Modules.DemoApi.Models.Claims;

/// <summary>
/// Insurance claim entity.
/// </summary>
public class Claim
{
    public Guid Id { get; set; }
    public required string ClaimantId { get; set; }
    public required string ClaimantName { get; set; }
    public required string PolicyNumber { get; set; }
    public decimal ClaimAmount { get; set; }
    public ClaimType ClaimType { get; set; }
    public ClaimStatus Status { get; set; }
    public required string Region { get; set; }
    public bool IsSensitive { get; set; }
    public string? AssignedAdjudicatorId { get; set; }
    public string? BusinessProcessId { get; set; }
    public DateTimeOffset FiledAt { get; set; }
    public DateTimeOffset? AdjudicatedAt { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? AdjudicationNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public enum ClaimType
{
    Auto,
    Home,
    Health,
    Life,
    Business
}

public enum ClaimStatus
{
    Draft,
    Filed,
    UnderReview,
    Approved,
    Rejected,
    Paid,
    Closed
}
