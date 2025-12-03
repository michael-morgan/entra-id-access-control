using Api.Modules.DemoApi.Models.Documents;
using Api.Modules.DemoApi.Services.Documents;
using Api.Modules.AccessControl.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.DemoApi.Controllers;

/// <summary>
/// Documents workstream controller.
/// ABAC Rules enforced:
/// - Confidential documents can only be accessed during business hours
/// - Department-scoped access control
/// - Classification-based permissions
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all documents (filtered by user's department).
    /// </summary>
    [HttpGet]
    [AuthorizeResource("Document", "list")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocuments(CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetDocumentsAsync(cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Get a specific document by ID.
    /// ABAC Rule: Confidential documents only accessible during business hours.
    /// </summary>
    [HttpGet("{id}")]
    [AuthorizeResource("Document/:id", "read")]
    public async Task<ActionResult<DocumentDto>> GetDocument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var document = await _documentService.GetDocumentAsync(id, cancellationToken);

            if (document == null)
                return NotFound(new { error = $"Document {id} not found" });

            return Ok(document);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document access attempt for document {DocumentId}", id);
            return Forbid();
        }
    }

    /// <summary>
    /// Upload a new document.
    /// </summary>
    [HttpPost]
    [AuthorizeResource("Document", "upload")]
    public async Task<ActionResult<DocumentDto>> UploadDocument(
        [FromBody] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var document = await _documentService.UploadDocumentAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document upload attempt");
            return Forbid();
        }
    }

    /// <summary>
    /// Update document metadata (title, classification).
    /// </summary>
    [HttpPatch("{id}")]
    [AuthorizeResource("Document/:id", "update")]
    public async Task<ActionResult<DocumentDto>> UpdateDocument(
        Guid id,
        [FromBody] UpdateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var document = await _documentService.UpdateDocumentAsync(id, request, cancellationToken);
            return Ok(document);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid document update attempt for document {DocumentId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document update attempt for document {DocumentId}", id);
            return Forbid();
        }
    }

    /// <summary>
    /// Download a document.
    /// ABAC Rule: Confidential documents only accessible during business hours.
    /// </summary>
    [HttpGet("{id}/download")]
    [AuthorizeResource("Document/:id", "download")]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _documentService.DownloadDocumentAsync(id, cancellationToken);

            // In production, fetch actual file from storage
            return File(content, "application/octet-stream", $"document-{id}.bin");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid document download attempt for document {DocumentId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document download attempt for document {DocumentId}", id);
            return Forbid();
        }
    }

    /// <summary>
    /// Delete a document.
    /// </summary>
    [HttpDelete("{id}")]
    [AuthorizeResource("Document/:id", "delete")]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.DeleteDocumentAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid document deletion attempt for document {DocumentId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized document deletion attempt for document {DocumentId}", id);
            return Forbid();
        }
    }
}
