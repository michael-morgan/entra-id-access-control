using System.Net.Http.Json;
using Api.Modules.AccessControl.Client.Configuration;
using Api.Modules.AccessControl.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Client.Http;

/// <summary>
/// HTTP client for calling the AccessControl authorization API.
/// JWT token handling is managed by AccessControlTokenHandler.
/// </summary>
public class AccessControlClient : IAccessControlClient
{
    private readonly HttpClient _httpClient;
    private readonly AccessControlClientOptions _options;
    private readonly ILogger<AccessControlClient> _logger;

    public AccessControlClient(
        HttpClient httpClient,
        IOptions<AccessControlClientOptions> options,
        ILogger<AccessControlClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthorizationCheckResponse> CheckAuthorizationAsync(
        string resource,
        string action,
        string? workstreamId = null,
        object? entityData = null,
        CancellationToken cancellationToken = default)
    {
        var request = new AuthorizationCheckRequest
        {
            Resource = resource,
            Action = action,
            WorkstreamId = workstreamId ?? _options.DefaultWorkstreamId,
            EntityData = entityData
        };

        _logger.LogDebug(
            "Checking authorization: Resource={Resource}, Action={Action}, Workstream={Workstream}",
            resource,
            action,
            request.WorkstreamId
        );

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authorization/check")
        {
            Content = JsonContent.Create(request)
        };

        // JWT token is added by AccessControlTokenHandler
        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<AuthorizationCheckResponse>(cancellationToken);
        if (response == null)
        {
            throw new InvalidOperationException("Failed to deserialize authorization response");
        }

        _logger.LogInformation(
            "Authorization {Result}: Resource={Resource}, Action={Action}, Reason={Reason}",
            response.Allowed ? "ALLOWED" : "DENIED",
            resource,
            action,
            response.Reason
        );

        return response;
    }

    /// <inheritdoc />
    public async Task<List<AuthorizationCheckResponse>> CheckBatchAuthorizationAsync(
        string workstreamId,
        List<ResourceActionCheck> checks,
        CancellationToken cancellationToken = default)
    {
        var request = new BatchAuthorizationCheckRequest
        {
            WorkstreamId = workstreamId,
            Checks = checks
        };

        _logger.LogDebug(
            "Checking batch authorization: Workstream={Workstream}, Count={Count}",
            workstreamId,
            checks.Count
        );

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authorization/check-batch")
        {
            Content = JsonContent.Create(request)
        };

        // JWT token is added by AccessControlTokenHandler
        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<List<AuthorizationCheckResponse>>(cancellationToken);
        if (response == null)
        {
            throw new InvalidOperationException("Failed to deserialize batch authorization response");
        }

        _logger.LogInformation(
            "Batch authorization complete: Total={Total}, Allowed={Allowed}, Denied={Denied}",
            response.Count,
            response.Count(r => r.Allowed),
            response.Count(r => !r.Allowed)
        );

        return response;
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthorizedAsync(
        string resource,
        string action,
        string? workstreamId = null,
        object? entityData = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await CheckAuthorizationAsync(
                resource,
                action,
                workstreamId,
                entityData,
                cancellationToken
            );

            return response.Allowed;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking authorization: Resource={Resource}, Action={Action}",
                resource,
                action
            );
            return false; // Deny on error (fail-secure)
        }
    }
}
