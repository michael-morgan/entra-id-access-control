using Api.Modules.DemoApi.Models.Documents;
using System.Collections.Concurrent;

namespace Api.Modules.DemoApi.Data;

/// <summary>
/// In-memory repository for Document entities.
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByClassificationAsync(DocumentClassification classification, CancellationToken cancellationToken = default);
    Task<Document> CreateAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = new();

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(id, out var document);
        return Task.FromResult(document);
    }

    public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Document>>(_documents.Values.ToList());
    }

    public Task<IReadOnlyList<Document>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        var documents = _documents.Values.Where(d => d.Department == department).ToList();
        return Task.FromResult<IReadOnlyList<Document>>(documents);
    }

    public Task<IReadOnlyList<Document>> GetByClassificationAsync(DocumentClassification classification, CancellationToken cancellationToken = default)
    {
        var documents = _documents.Values.Where(d => d.Classification == classification).ToList();
        return Task.FromResult<IReadOnlyList<Document>>(documents);
    }

    public Task<Document> CreateAsync(Document document, CancellationToken cancellationToken = default)
    {
        document.CreatedAt = DateTimeOffset.UtcNow;
        _documents[document.Id] = document;
        return Task.FromResult(document);
    }

    public Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        document.ModifiedAt = DateTimeOffset.UtcNow;
        _documents[document.Id] = document;
        return Task.FromResult(document);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _documents.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
