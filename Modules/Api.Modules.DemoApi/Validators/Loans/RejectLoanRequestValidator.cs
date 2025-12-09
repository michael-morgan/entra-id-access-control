using Api.Modules.DemoApi.Models.Loans;
using FluentValidation;

namespace Api.Modules.DemoApi.Validators.Loans;

public class RejectLoanRequestValidator : AbstractValidator<RejectLoanRequest>
{
    public RejectLoanRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Rejection reason is required")
            .MinimumLength(10).WithMessage("Rejection reason must be at least 10 characters")
            .MaximumLength(1000).WithMessage("Rejection reason cannot exceed 1000 characters");
    }
}
