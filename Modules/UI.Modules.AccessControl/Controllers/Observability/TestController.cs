using System.Text.Json;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Services;
using UI.Modules.AccessControl.Services.Graph;
using UI.Modules.AccessControl.Services.Testing;
using UI.Modules.AccessControl.Services.Attributes;
using UI.Modules.AccessControl.Services.Authorization.Policies;
using UI.Modules.AccessControl.Services.Authorization.Roles;
using UI.Modules.AccessControl.Services.Authorization.Resources;
using UI.Modules.AccessControl.Services.Authorization.AbacRules;
using UI.Modules.AccessControl.Services.Authorization.Users;
using UI.Modules.AccessControl.Services.Audit;

namespace UI.Modules.AccessControl.Controllers.Observability;

/// <summary>
/// Controller for testing authorization with JWT tokens and viewing all associated access control data.
/// </summary>
public class TestController(
    ITokenAnalysisService tokenAnalysisService,
    IAuthorizationTestingService authorizationTestingService,
    IScenarioTestingService scenarioTestingService,
    ILogger<TestController> logger) : Controller
{
    private readonly ITokenAnalysisService _tokenAnalysisService = tokenAnalysisService;
    private readonly IAuthorizationTestingService _authorizationTestingService = authorizationTestingService;
    private readonly IScenarioTestingService _scenarioTestingService = scenarioTestingService;
    private readonly ILogger<TestController> _logger = logger;

    // GET: Test
    public IActionResult Index()
    {
        return View();
    }

    // POST: Test/DecodeToken
    [HttpPost]
    [IgnoreAntiforgeryToken] // JSON API endpoint - uses header-based CSRF protection in production
    public async Task<IActionResult> DecodeToken([FromBody] DecodeTokenRequest request)
    {
        var result = await _tokenAnalysisService.AnalyzeTokenAsync(request.Token, request.WorkstreamId);
        return Json(result);
    }

    private static string FormatPolicyDisplay(CasbinPolicy policy)
    {
        return policy.PolicyType switch
        {
            "p" => $"{policy.V0}, {policy.V1}, {policy.V2}",
            "g" => $"{policy.V0} → {policy.V1}" + (policy.V2 != null ? $" ({policy.V2})" : ""),
            "g2" => $"{policy.V0} → {policy.V1} in domain {policy.V2}",
            _ => $"{policy.V0}, {policy.V1}, {policy.V2}"
        };
    }

    private string FormatAbacRuleDisplay(AbacRule rule)
    {
        var display = $"{rule.RuleType}: {rule.RuleName}";

        // Try to parse configuration JSON for additional details
        if (!string.IsNullOrWhiteSpace(rule.Configuration))
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rule.Configuration);
                if (config != null)
                {
                    // Try to extract common configuration fields
                    if (config.TryGetValue("operator", out JsonElement operatorValue))
                    {
                        display += $" {operatorValue.GetString()}";
                    }

                    if (config.TryGetValue("userAttribute", out JsonElement userAttrValue))
                    {
                        display += $" user.{userAttrValue.GetString()}";
                    }

                    if (config.TryGetValue("resourceProperty", out JsonElement resourcePropValue))
                    {
                        display += $" resource.{resourcePropValue.GetString()}";
                    }

                    if (config.ContainsKey("min") || config.ContainsKey("max"))
                    {
                        var min = config.TryGetValue("min", out JsonElement minValue) ? minValue.ToString() : "?";
                        var max = config.TryGetValue("max", out JsonElement maxValue) ? maxValue.ToString() : "?";
                        display += $" [{min} to {max}]";
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse ABAC rule configuration for rule {RuleName}", rule.RuleName);
                display += " (see configuration)";
            }
        }

        return display;
    }

    // POST: Test/CheckAuthorization
    [HttpPost]
    [IgnoreAntiforgeryToken] // JSON API endpoint - uses header-based CSRF protection in production
    public async Task<IActionResult> CheckAuthorization([FromBody] AuthorizationTestRequest request)
    {
        var result = await _authorizationTestingService.CheckAuthorizationAsync(request);
        return Json(result);
    }

    // POST: Test/RunScenario
    [HttpPost]
    [IgnoreAntiforgeryToken] // JSON API endpoint - uses header-based CSRF protection in production
    public async Task<IActionResult> RunScenario([FromBody] RunScenarioRequest request)
    {
        var result = await _scenarioTestingService.RunScenarioAsync(request.ScenarioName, request.Token, request.WorkstreamId);
        return Json(result);
    }

    // POST: Test/GetAvailableScenarios
    [HttpPost]
    [IgnoreAntiforgeryToken] // JSON API endpoint - uses header-based CSRF protection in production
    public async Task<IActionResult> GetAvailableScenarios([FromBody] GetScenariosRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.WorkstreamId))
        {
            return Json(new { success = false, errorMessage = "Token and workstream ID are required." });
        }

        try
        {
            var scenarios = await _scenarioTestingService.GetAvailableScenariosAsync(request.Token, request.WorkstreamId);
            return Json(new { success = true, scenarios });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating scenarios for workstream {WorkstreamId}", request.WorkstreamId);
            return Json(new { success = false, errorMessage = $"Error generating scenarios: {ex.Message}" });
        }
    }
}

public class DecodeTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string WorkstreamId { get; set; } = string.Empty;
}

public class RunScenarioRequest
{
    public string Token { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public string WorkstreamId { get; set; } = string.Empty;
}

public class GetScenariosRequest
{
    public string Token { get; set; } = string.Empty;
    public string WorkstreamId { get; set; } = string.Empty;
}

