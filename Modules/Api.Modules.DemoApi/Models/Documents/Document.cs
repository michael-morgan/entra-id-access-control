namespace Api.Modules.DemoApi.Models.Documents;

/// <summary>
/// Document entity.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string FileName { get; set; }
    public required string Department { get; set; }
    public DocumentClassification Classification { get; set; }
    public required string UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
    public string? LastAccessedBy { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? StoragePath { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public enum DocumentClassification
{
    Public,
    Internal,
    Confidential,
    Restricted
}
