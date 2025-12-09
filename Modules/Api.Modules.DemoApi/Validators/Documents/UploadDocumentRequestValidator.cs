using Api.Modules.DemoApi.Models.Documents;
using FluentValidation;

namespace Api.Modules.DemoApi.Validators.Documents;

public class UploadDocumentRequestValidator : AbstractValidator<UploadDocumentRequest>
{
    public UploadDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required")
            .MaximumLength(200).WithMessage("Document title cannot exceed 200 characters");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters")
            .Must(HaveValidFileExtension).WithMessage("File extension is invalid or potentially dangerous");

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Department is required")
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters");

        RuleFor(x => x.Classification)
            .IsInEnum().WithMessage("Invalid classification");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than zero")
            .LessThanOrEqualTo(100_000_000).WithMessage("File size cannot exceed 100 MB");

        RuleFor(x => x.ContentType)
            .MaximumLength(100).WithMessage("Content type cannot exceed 100 characters")
            .Must(BeValidContentType).When(x => !string.IsNullOrEmpty(x.ContentType))
            .WithMessage("Invalid or potentially dangerous content type");
    }

    private static bool HaveValidFileExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Disallow dangerous extensions
        var dangerousExtensions = new[] { ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jar" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return !dangerousExtensions.Contains(extension);
    }

    private static bool BeValidContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return true; // Optional field

        // Basic MIME type validation
        return contentType.Contains('/') &&
               !contentType.Contains("application/x-msdownload") &&
               !contentType.Contains("application/x-executable");
    }
}
