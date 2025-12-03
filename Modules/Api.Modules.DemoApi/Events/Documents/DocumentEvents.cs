using Api.Modules.AccessControl.Models;

namespace Api.Modules.DemoApi.Events.Documents;

/// <summary>
/// Business event: Document uploaded.
/// </summary>
public record DocumentUploaded : BusinessEvent
{
    public override string EventCategory => "Document";

    public Guid DocumentId { get; init; }
    public required string Title { get; init; }
    public required string FileName { get; init; }
    public required string Department { get; init; }
    public required string Classification { get; init; }
    public long FileSizeBytes { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Document", DocumentId.ToString())
    };
}

/// <summary>
/// Business event: Document accessed (viewed or downloaded).
/// </summary>
public record DocumentAccessed : BusinessEvent
{
    public override string EventCategory => "Document";

    public Guid DocumentId { get; init; }
    public required string AccessType { get; init; } // "View" or "Download"

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Document", DocumentId.ToString())
    };
}

/// <summary>
/// Business event: Document classification changed.
/// </summary>
public record DocumentClassificationChanged : BusinessEvent
{
    public override string EventCategory => "Document";

    public Guid DocumentId { get; init; }
    public required string PreviousClassification { get; init; }
    public required string NewClassification { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Document", DocumentId.ToString())
    };
}

/// <summary>
/// Business event: Document deleted.
/// </summary>
public record DocumentDeleted : BusinessEvent
{
    public override string EventCategory => "Document";

    public Guid DocumentId { get; init; }
    public required string Title { get; init; }
    public required string Department { get; init; }

    public override IReadOnlyList<AffectedEntity> AffectedEntities => new[]
    {
        new AffectedEntity("Document", DocumentId.ToString())
    };
}
