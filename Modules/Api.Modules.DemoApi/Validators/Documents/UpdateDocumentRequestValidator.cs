using Api.Modules.DemoApi.Models.Documents;
using FluentValidation;

namespace Api.Modules.DemoApi.Validators.Documents;

public class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequest>
{
    public UpdateDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Document title cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Classification)
            .IsInEnum().WithMessage("Invalid classification")
            .When(x => x.Classification.HasValue);
    }
}
