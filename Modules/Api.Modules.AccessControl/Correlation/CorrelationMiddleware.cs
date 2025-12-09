using Api.Modules.AccessControl.Constants;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Correlation;

/// <summary>
/// Middleware that extracts/generates correlation IDs and enriches logging.
/// </summary>
public class CorrelationMiddleware(
    ICorrelationContextAccessor accessor,
    IOptions<CorrelationOptions> options,
    ILogger<CorrelationMiddleware> logger) : IMiddleware
{
    private readonly ICorrelationContextAccessor _accessor = accessor;
    private readonly IOptions<CorrelationOptions> _options = options;
    private readonly ILogger<CorrelationMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var options = _options.Value;

        // Extract or generate correlation IDs
        var businessProcessId = context.Request.Headers[HeaderNames.BusinessProcessId].FirstOrDefault();
        var sessionCorrelationId = context.Request.Headers[HeaderNames.SessionCorrelationId].FirstOrDefault();
        var requestId = context.Request.Headers[HeaderNames.RequestId].FirstOrDefault();
        var workstreamId = context.Request.Headers[HeaderNames.WorkstreamId].FirstOrDefault();

        // Generate request ID if missing
        if (string.IsNullOrWhiteSpace(requestId) && options.GenerateRequestIdIfMissing)
        {
            requestId = Guid.NewGuid().ToString("N");
        }

        // Generate session correlation ID if missing (backend manages this)
        if (string.IsNullOrWhiteSpace(sessionCorrelationId))
        {
            sessionCorrelationId = Guid.NewGuid().ToString("N");
        }

        // Use default workstream if not provided
        if (string.IsNullOrWhiteSpace(workstreamId) && !string.IsNullOrWhiteSpace(options.DefaultWorkstreamId))
        {
            workstreamId = options.DefaultWorkstreamId;
        }

        // Create correlation context
        var correlationContext = new CorrelationContext
        {
            BusinessProcessId = businessProcessId,
            SessionCorrelationId = sessionCorrelationId,
            RequestCorrelationId = requestId ?? Guid.NewGuid().ToString("N"),
            WorkstreamId = workstreamId ?? "default",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Store in accessor for downstream use
        _accessor.Context = correlationContext;

        // Enrich logging scope
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestCorrelationId"] = correlationContext.RequestCorrelationId,
            ["WorkstreamId"] = correlationContext.WorkstreamId,
            ["BusinessProcessId"] = correlationContext.BusinessProcessId ?? "none",
            ["SessionCorrelationId"] = correlationContext.SessionCorrelationId ?? "none"
        });

        // Add to response headers if configured
        if (options.IncludeInResponse)
        {
            context.Response.Headers.TryAdd(HeaderNames.RequestId, correlationContext.RequestCorrelationId);
            context.Response.Headers.TryAdd(HeaderNames.SessionCorrelationId, correlationContext.SessionCorrelationId!);
            if (!string.IsNullOrWhiteSpace(correlationContext.BusinessProcessId))
            {
                context.Response.Headers.TryAdd(HeaderNames.BusinessProcessId, correlationContext.BusinessProcessId);
            }
        }

        await next(context);
    }
}
