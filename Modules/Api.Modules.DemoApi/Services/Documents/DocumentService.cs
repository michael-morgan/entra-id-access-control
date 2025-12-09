using Api.Modules.DemoApi.Data;
using Api.Modules.DemoApi.Events.Documents;
using Api.Modules.DemoApi.Models.Documents;
using Api.Modules.AccessControl.Interfaces;

namespace Api.Modules.DemoApi.Services.Documents;

/// <summary>
/// Business logic for document operations.
/// ABAC Rules:
/// - Confidential documents can only be accessed during business hours
/// - Department-scoped access control
/// - Classification-based permissions
/// </summary>
public interface IDocumentService
{
    Task<DocumentDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default);
    Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);
    Task<DocumentDto> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadDocumentAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
}

public class DocumentService(
    IDocumentRepository repository,
    IAuthorizationEnforcer enforcer,
    IBusinessEventPublisher eventPublisher,
    ICurrentUserAccessor currentUser,
    IUserAttributeStore userAttributeStore) : IDocumentService
{
    private readonly IDocumentRepository _repository = repository;
    private readonly IAuthorizationEnforcer _enforcer = enforcer;
    private readonly IBusinessEventPublisher _eventPublisher = eventPublisher;
    private readonly ICurrentUserAccessor _currentUser = currentUser;
    private readonly IUserAttributeStore _userAttributeStore = userAttributeStore;

    public async Task<DocumentDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);
        if (document == null)
            return null;

        // Authorization check with entity data (checks business hours, department, classification)
        await _enforcer.EnsureAuthorizedAsync($"Document/{id}", "read", document);

        // Update last accessed timestamp
        var user = _currentUser.User;
        document.LastAccessedAt = DateTimeOffset.UtcNow;
        document.LastAccessedBy = user.Id;
        await _repository.UpdateAsync(document, cancellationToken);

        // Publish access event
        await _eventPublisher.PublishAsync(
            new DocumentAccessed
            {
                DocumentId = document.Id,
                AccessType = "View"
            },
            justification: $"Document viewed by {user.DisplayName ?? user.Id}",
            cancellationToken);

        return DocumentDto.FromEntity(document);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        // Coarse-grained check
        await _enforcer.EnsureAuthorizedAsync("Document", "list", null);

        var documents = await _repository.GetAllAsync(cancellationToken);

        // Fine-grained filtering based on user attributes
        var userId = _currentUser.User.Id;
        var workstreamId = "documents";
        var userAttributes = await _userAttributeStore.GetAttributesAsync(userId, workstreamId, cancellationToken);

        // Extract Department from dynamic attributes
        string? userDepartment = null;
        if (userAttributes?.Attributes.TryGetValue("Department", out var deptElement) == true)
        {
            userDepartment = deptElement.ToString();
        }

        var filteredDocuments = documents.Where(doc =>
        {
            // Department-scoped access
            if (userDepartment != null && doc.Department != userDepartment)
                return false;

            return true;
        }).ToList();

        return filteredDocuments.Select(DocumentDto.FromEntity).ToList();
    }

    public async Task<DocumentDto> UploadDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        // Authorization check
        await _enforcer.EnsureAuthorizedAsync("Document", "upload", null);

        var user = _currentUser.User;

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            FileName = request.FileName,
            Department = request.Department,
            Classification = request.Classification,
            UploadedBy = user.Id,
            UploadedAt = DateTimeOffset.UtcNow,
            FileSizeBytes = request.FileSizeBytes,
            ContentType = request.ContentType,
            StoragePath = $"/documents/{request.Department}/{Guid.NewGuid()}/{request.FileName}"
        };

        await _repository.CreateAsync(document, cancellationToken);

        // Publish business event
        await _eventPublisher.PublishAsync(
            new DocumentUploaded
            {
                DocumentId = document.Id,
                Title = document.Title,
                FileName = document.FileName,
                Department = document.Department,
                Classification = document.Classification.ToString(),
                FileSizeBytes = document.FileSizeBytes
            },
            justification: $"Document uploaded by {user.DisplayName ?? user.Id}",
            cancellationToken);

        return DocumentDto.FromEntity(document);
    }

    public async Task<DocumentDto> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Document {id} not found");

        // Authorization check with entity data
        await _enforcer.EnsureAuthorizedAsync($"Document/{id}", "update", document);

        var previousClassification = document.Classification;

        if (request.Title != null)
            document.Title = request.Title;

        if (request.Classification.HasValue && request.Classification.Value != document.Classification)
        {
            document.Classification = request.Classification.Value;

            // Publish classification change event
            await _eventPublisher.PublishAsync(
                new DocumentClassificationChanged
                {
                    DocumentId = document.Id,
                    PreviousClassification = previousClassification.ToString(),
                    NewClassification = document.Classification.ToString()
                },
                justification: $"Classification changed from {previousClassification} to {document.Classification}",
                cancellationToken);
        }

        await _repository.UpdateAsync(document, cancellationToken);

        return DocumentDto.FromEntity(document);
    }

    public async Task<byte[]> DownloadDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Document {id} not found");

        // Authorization check with entity data (checks business hours for confidential docs)
        await _enforcer.EnsureAuthorizedAsync($"Document/{id}", "download", document);

        // Update last accessed timestamp
        var user = _currentUser.User;
        document.LastAccessedAt = DateTimeOffset.UtcNow;
        document.LastAccessedBy = user.Id;
        await _repository.UpdateAsync(document, cancellationToken);

        // Publish access event
        await _eventPublisher.PublishAsync(
            new DocumentAccessed
            {
                DocumentId = document.Id,
                AccessType = "Download"
            },
            justification: $"Document downloaded by {user.DisplayName ?? user.Id}",
            cancellationToken);

        // For demo purposes, return dummy content
        return System.Text.Encoding.UTF8.GetBytes($"[Content of {document.FileName}]");
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Document {id} not found");

        // Authorization check
        await _enforcer.EnsureAuthorizedAsync($"Document/{id}", "delete", document);

        await _repository.DeleteAsync(id, cancellationToken);

        // Publish business event
        await _eventPublisher.PublishAsync(
            new DocumentDeleted
            {
                DocumentId = document.Id,
                Title = document.Title,
                Department = document.Department
            },
            justification: $"Document deleted by {_currentUser.User.DisplayName ?? _currentUser.User.Id}",
            cancellationToken);
    }
}
