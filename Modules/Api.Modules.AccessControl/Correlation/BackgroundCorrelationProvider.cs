using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Correlation;

/// <summary>
/// Creates and manages correlation context for background operations.
/// </summary>
public class BackgroundCorrelationProvider(
    ICorrelationContextAccessor accessor,
    ILogger<BackgroundCorrelationProvider> logger) : IBackgroundCorrelationProvider
{
    private readonly ICorrelationContextAccessor _accessor = accessor;
    private readonly ILogger<BackgroundCorrelationProvider> _logger = logger;

    public CorrelationContext CreateBackgroundContext(
        string workstreamId,
        string? businessProcessId = null,
        string? jobName = null)
    {
        return new CorrelationContext
        {
            BusinessProcessId = businessProcessId,
            SessionCorrelationId = $"bg-{jobName ?? "job"}",
            RequestCorrelationId = Guid.NewGuid().ToString("N"),
            WorkstreamId = workstreamId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public async Task ExecuteWithCorrelationAsync(
        CorrelationContext context,
        Func<Task> action)
    {
        var previousContext = _accessor.Context;

        try
        {
            _accessor.Context = context;

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestCorrelationId"] = context.RequestCorrelationId,
                ["WorkstreamId"] = context.WorkstreamId,
                ["BusinessProcessId"] = context.BusinessProcessId ?? "none",
                ["IsBackground"] = true
            });

            await action();
        }
        finally
        {
            _accessor.Context = previousContext;
        }
    }
}
