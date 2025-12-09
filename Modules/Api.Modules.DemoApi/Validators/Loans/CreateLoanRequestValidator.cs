using Api.Modules.DemoApi.Models.Loans;
using FluentValidation;

namespace Api.Modules.DemoApi.Validators.Loans;

public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.ApplicantId)
            .NotEmpty().WithMessage("Applicant ID is required")
            .MaximumLength(100).WithMessage("Applicant ID cannot exceed 100 characters");

        RuleFor(x => x.ApplicantName)
            .NotEmpty().WithMessage("Applicant name is required")
            .MaximumLength(200).WithMessage("Applicant name cannot exceed 200 characters")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("Applicant name contains invalid characters");

        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0).WithMessage("Requested amount must be greater than zero")
            .LessThanOrEqualTo(10_000_000).WithMessage("Requested amount cannot exceed $10,000,000");

        RuleFor(x => x.TermMonths)
            .GreaterThan(0).WithMessage("Term must be at least 1 month")
            .LessThanOrEqualTo(360).WithMessage("Term cannot exceed 360 months (30 years)");

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
