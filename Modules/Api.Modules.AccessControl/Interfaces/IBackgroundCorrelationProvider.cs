using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Creates correlation context for background operations.
/// </summary>
public interface IBackgroundCorrelationProvider
{
    /// <summary>
    /// Creates a new correlation context for a background job.
    /// </summary>
    CorrelationContext CreateBackgroundContext(
        string workstreamId,
        string? businessProcessId = null,
        string? jobName = null);

    /// <summary>
    /// Executes an action within a correlation scope.
    /// </summary>
    Task ExecuteWithCorrelationAsync(
        CorrelationContext context,
        Func<Task> action);
}
