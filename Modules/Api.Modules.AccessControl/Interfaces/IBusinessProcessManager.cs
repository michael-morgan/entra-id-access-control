using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Manages business process lifecycle.
/// </summary>
public interface IBusinessProcessManager
{
    /// <summary>
    /// Initiates a new business process and returns its identifier.
    /// Call this when a workflow begins (e.g., "Start New Application").
    /// </summary>
    Task<BusinessProcess> InitiateProcessAsync(
        string processType,
        string workstreamId,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an existing process for continuation.
    /// </summary>
    Task<BusinessProcess?> GetProcessAsync(
        string businessProcessId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates process metadata without changing status.
    /// </summary>
    Task UpdateProcessMetadataAsync(
        string businessProcessId,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a process as completed with a terminal state.
    /// </summary>
    Task CompleteProcessAsync(
        string businessProcessId,
        BusinessProcessOutcome outcome,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the timeline of events for a business process.
    /// </summary>
    Task<IReadOnlyList<BusinessEventSummary>> GetProcessTimelineAsync(
        string businessProcessId,
        CancellationToken cancellationToken = default);
}
