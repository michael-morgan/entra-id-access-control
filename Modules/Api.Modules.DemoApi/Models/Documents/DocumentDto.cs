namespace Api.Modules.DemoApi.Models.Documents;

/// <summary>
/// Data transfer object for Document.
/// </summary>
public record DocumentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Classification { get; init; } = string.Empty;
    public string UploadedBy { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt { get; init; }
    public DateTimeOffset? LastAccessedAt { get; init; }
    public string? LastAccessedBy { get; init; }
    public long FileSizeBytes { get; init; }
    public string? ContentType { get; init; }

    public static DocumentDto FromEntity(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        FileName = document.FileName,
        Department = document.Department,
        Classification = document.Classification.ToString(),
        UploadedBy = document.UploadedBy,
        UploadedAt = document.UploadedAt,
        LastAccessedAt = document.LastAccessedAt,
        LastAccessedBy = document.LastAccessedBy,
        FileSizeBytes = document.FileSizeBytes,
        ContentType = document.ContentType
    };
}

public record UploadDocumentRequest
{
    public required string Title { get; init; }
    public required string FileName { get; init; }
    public required string Department { get; init; }
    public DocumentClassification Classification { get; init; }
    public long FileSizeBytes { get; init; }
    public string? ContentType { get; init; }
}

public record UpdateDocumentRequest
{
    public string? Title { get; init; }
    public DocumentClassification? Classification { get; init; }
}
