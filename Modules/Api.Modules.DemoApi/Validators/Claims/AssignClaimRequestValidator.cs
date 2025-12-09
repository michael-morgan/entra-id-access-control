using Api.Modules.DemoApi.Models.Claims;
using FluentValidation;

namespace Api.Modules.DemoApi.Validators.Claims;

public class AssignClaimRequestValidator : AbstractValidator<AssignClaimRequest>
{
    public AssignClaimRequestValidator()
    {
        RuleFor(x => x.AdjudicatorId)
            .NotEmpty().WithMessage("Adjudicator ID is required")
            .MaximumLength(100).WithMessage("Adjudicator ID cannot exceed 100 characters");
    }
}
