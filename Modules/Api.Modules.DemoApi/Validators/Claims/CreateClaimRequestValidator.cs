using Api.Modules.DemoApi.Models.Claims;
using FluentValidation;

namespace Api.Modules.DemoApi.Validators.Claims;

public class CreateClaimRequestValidator : AbstractValidator<CreateClaimRequest>
{
    public CreateClaimRequestValidator()
    {
        RuleFor(x => x.ClaimantId)
            .NotEmpty().WithMessage("Claimant ID is required")
            .MaximumLength(100).WithMessage("Claimant ID cannot exceed 100 characters");

        RuleFor(x => x.ClaimantName)
            .NotEmpty().WithMessage("Claimant name is required")
            .MaximumLength(200).WithMessage("Claimant name cannot exceed 200 characters")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("Claimant name contains invalid characters");

        RuleFor(x => x.PolicyNumber)
            .NotEmpty().WithMessage("Policy number is required")
            .MaximumLength(50).WithMessage("Policy number cannot exceed 50 characters")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Policy number must contain only uppercase letters, numbers, and hyphens");

        RuleFor(x => x.ClaimAmount)
            .GreaterThan(0).WithMessage("Claim amount must be greater than zero")
            .LessThanOrEqualTo(5_000_000).WithMessage("Claim amount cannot exceed $5,000,000");

        RuleFor(x => x.ClaimType)
            .IsInEnum().WithMessage("Invalid claim type");

        RuleFor(x => x.Region)
            .NotEmpty().WithMessage("Region is required")
            .MaximumLength(50).WithMessage("Region cannot exceed 50 characters")
            .Must(BeValidRegion).WithMessage("Invalid region. Valid regions are: US-WEST, US-EAST, US-CENTRAL, US-SOUTH");
    }

    private static bool BeValidRegion(string region)
    {
        var validRegions = new[] { "US-WEST", "US-EAST", "US-CENTRAL", "US-SOUTH" };
        return validRegions.Contains(region, StringComparer.OrdinalIgnoreCase);
    }
}
