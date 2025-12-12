using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Modules.AccessControl.Client.Configuration;
using Api.Modules.AccessControl.Client.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Client.Http;

/// <summary>
/// HTTP client for calling the AccessControl authorization API.
/// Handles JWT forwarding, error handling, and retry logic.
/// </summary>
public class AccessControlClient : IAccessControlClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AccessControlClientOptions _options;
    private readonly ILogger<AccessControlClient> _logger;

    public AccessControlClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AccessControlClientOptions> options,
        ILogger<AccessControlClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _logger = logger;

        // Configure base URL
        if (!string.IsNullOrWhiteSpace(_options.ApiBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.ApiBaseUrl);
        }

        // Configure timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
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

        AddAuthorizationHeader(httpRequest);

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

        AddAuthorizationHeader(httpRequest);

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

    /// <summary>
    /// Adds Authorization header with JWT token from current HTTP context.
    /// </summary>
    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        if (!_options.ForwardJwtToken)
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, cannot forward JWT token");
            return;
        }

        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            _logger.LogWarning("Authorization header not found in current request");
            return;
        }

        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        _logger.LogDebug("JWT token forwarded to authorization API");
    }
}
