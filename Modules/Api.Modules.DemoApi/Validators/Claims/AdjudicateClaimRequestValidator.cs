using Api.Modules.DemoApi.Models.Claims;
using FluentValidation;

namespace Api.Modules.DemoApi.Validators.Claims;

public class AdjudicateClaimRequestValidator : AbstractValidator<AdjudicateClaimRequest>
{
    public AdjudicateClaimRequestValidator()
    {
        RuleFor(x => x.ApprovedAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Approved amount cannot be negative")
            .LessThanOrEqualTo(5_000_000).WithMessage("Approved amount cannot exceed $5,000,000");

        RuleFor(x => x.AdjudicationNotes)
            .NotEmpty().WithMessage("Adjudication notes are required")
            .MinimumLength(10).WithMessage("Adjudication notes must be at least 10 characters")
            .MaximumLength(1000).WithMessage("Adjudication notes cannot exceed 1000 characters");
    }
}
