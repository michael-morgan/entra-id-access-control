namespace Api.Modules.DemoApi.Models.Claims;

/// <summary>
/// Data transfer object for Claim.
/// </summary>
public record ClaimDto
{
    public Guid Id { get; init; }
    public string ClaimantId { get; init; } = string.Empty;
    public string ClaimantName { get; init; } = string.Empty;
    public string PolicyNumber { get; init; } = string.Empty;
    public decimal ClaimAmount { get; init; }
    public string ClaimType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public bool IsSensitive { get; init; }
    public string? AssignedAdjudicatorId { get; init; }
    public string? BusinessProcessId { get; init; }
    public DateTimeOffset FiledAt { get; init; }
    public DateTimeOffset? AdjudicatedAt { get; init; }
    public decimal? ApprovedAmount { get; init; }
    public string? AdjudicationNotes { get; init; }

    public static ClaimDto FromEntity(Claim claim) => new()
    {
        Id = claim.Id,
        ClaimantId = claim.ClaimantId,
        ClaimantName = claim.ClaimantName,
        PolicyNumber = claim.PolicyNumber,
        ClaimAmount = claim.ClaimAmount,
        ClaimType = claim.ClaimType.ToString(),
        Status = claim.Status.ToString(),
        Region = claim.Region,
        IsSensitive = claim.IsSensitive,
        AssignedAdjudicatorId = claim.AssignedAdjudicatorId,
        BusinessProcessId = claim.BusinessProcessId,
        FiledAt = claim.FiledAt,
        AdjudicatedAt = claim.AdjudicatedAt,
        ApprovedAmount = claim.ApprovedAmount,
        AdjudicationNotes = claim.AdjudicationNotes
    };
}

public record CreateClaimRequest
{
    public required string ClaimantId { get; init; }
    public required string ClaimantName { get; init; }
    public required string PolicyNumber { get; init; }
    public decimal ClaimAmount { get; init; }
    public ClaimType ClaimType { get; init; }
    public required string Region { get; init; }
    public bool IsSensitive { get; init; }
}

public record AdjudicateClaimRequest
{
    public decimal ApprovedAmount { get; init; }
    public required string AdjudicationNotes { get; init; }
    public bool IsApproved { get; init; }
}

public record AssignClaimRequest
{
    public required string AdjudicatorId { get; init; }
}
