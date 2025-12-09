using Api.Modules.DemoApi.Models.Loans;
using FluentValidation;

namespace Api.Modules.DemoApi.Validators.Loans;

public class ApproveLoanRequestValidator : AbstractValidator<ApproveLoanRequest>
{
    public ApproveLoanRequestValidator()
    {
        RuleFor(x => x.ApprovedAmount)
            .GreaterThan(0).WithMessage("Approved amount must be greater than zero")
            .LessThanOrEqualTo(10_000_000).WithMessage("Approved amount cannot exceed $10,000,000");

        RuleFor(x => x.InterestRate)
            .GreaterThanOrEqualTo(0).WithMessage("Interest rate cannot be negative")
            .LessThanOrEqualTo(100).WithMessage("Interest rate cannot exceed 100%");

        RuleFor(x => x.ApprovalNotes)
            .MaximumLength(1000).WithMessage("Approval notes cannot exceed 1000 characters");
    }
}
