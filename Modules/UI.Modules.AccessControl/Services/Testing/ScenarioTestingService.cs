using System.IdentityModel.Tokens.Jwt;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Testing;

/// <summary>
/// Service for running predefined authorization test scenarios.
/// </summary>
public class ScenarioTestingService(
    IPolicyRepository policyRepository,
    IAuthorizationTestingService authorizationTestingService,
    ILogger<ScenarioTestingService> logger) : IScenarioTestingService
{
    private readonly IPolicyRepository _policyRepository = policyRepository;
    private readonly IAuthorizationTestingService _authorizationTestingService = authorizationTestingService;
    private readonly ILogger<ScenarioTestingService> _logger = logger;

    public async Task<List<DynamicScenario>> GetAvailableScenariosAsync(string token, string workstreamId)
    {
        // Decode token to get user info
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
        var groups = jwtToken.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
        var roles = jwtToken.Claims.Where(c => c.Type == "roles" || c.Type == "role").Select(c => c.Value).ToList();

        _logger.LogInformation("Generating scenarios for workstream {WorkstreamId} - UserId: {UserId}, Groups: [{Groups}], Roles: [{Roles}]",
            workstreamId, userId, string.Join(", ", groups), string.Join(", ", roles));

        // Build initial subjects for Casbin
        var subjects = new List<string>();
        if (!string.IsNullOrWhiteSpace(userId)) subjects.Add(userId);
        subjects.AddRange(roles.Where(r => !string.IsNullOrWhiteSpace(r)));
        subjects.AddRange(groups.Where(g => !string.IsNullOrWhiteSpace(g)));

        _logger.LogInformation("Initial subjects: [{Subjects}]", string.Join(", ", subjects));

        // Resolve group-to-role mappings (type "g" policies)
        var roleMappings = (await _policyRepository.GetBySubjectIdsAsync(subjects, policyType: "g"))
            .Where(p => p.WorkstreamId == workstreamId || p.WorkstreamId == "*")
            .ToList();

        var resolvedRoles = roleMappings
            .Where(g => !string.IsNullOrWhiteSpace(g.V1))
            .Select(g => g.V1!)
            .Distinct()
            .ToList();

        _logger.LogInformation("Resolved {Count} roles from group mappings: [{Roles}]", resolvedRoles.Count, string.Join(", ", resolvedRoles));

        // Add resolved roles to subjects
        subjects.AddRange(resolvedRoles);

        _logger.LogInformation("Final subjects for policy query: [{Subjects}]", string.Join(", ", subjects));

        // Get all permission policies for this workstream and resolved subjects
        var policies = (await _policyRepository.GetBySubjectIdsAsync(subjects, policyType: "p"))
            .Where(p => p.WorkstreamId == workstreamId || p.WorkstreamId == "*")
            .ToList();

        _logger.LogInformation("Found {Count} permission policies for subjects", policies.Count);

        // Log the actual policies for debugging
        foreach (var policy in policies.Where(p => p.PolicyType == "p"))
        {
            _logger.LogInformation("Policy: Subject={Subject}, Workstream={Workstream}, Resource={Resource}, Action={Action}, Effect={Effect}",
                policy.V0, policy.V1, policy.V2, policy.V3, policy.V4);
        }

        // Extract unique resource/action combinations
        // V0=Subject, V1=Workstream, V2=Resource, V3=Action, V4=Effect
        var resourceActions = policies
            .Where(p => !string.IsNullOrWhiteSpace(p.V2) && !string.IsNullOrWhiteSpace(p.V3))
            .Select(p => new { Resource = p.V2!, Action = p.V3! })
            .Distinct()
            .ToList();

        _logger.LogInformation("Extracted {Count} unique resource/action combinations", resourceActions.Count);
        foreach (var ra in resourceActions)
        {
            _logger.LogInformation("Resource/Action: {Resource} / {Action}", ra.Resource, ra.Action);
        }

        var scenarios = new List<DynamicScenario>();

        // Group by resource to create scenarios
        var resourceGroups = resourceActions.GroupBy(ra => ra.Resource);

        foreach (var resourceGroup in resourceGroups)
        {
            var resource = resourceGroup.Key;
            var actions = resourceGroup.Select(ra => ra.Action).ToList();

            // Create a scenario for this resource showing what actions are allowed
            // Use Base64 encoding for the resource to preserve exact casing and special characters
            var resourceId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(resource));
            var scenario = new DynamicScenario
            {
                Id = $"{workstreamId}-{resourceId}",
                Name = $"{resource} - Authorized Actions",
                Description = $"Test all authorized actions for {resource} in {workstreamId} workstream",
                Resource = resource,
                AvailableActions = actions,
                WorkstreamId = workstreamId
            };

            scenarios.Add(scenario);
        }

        return scenarios;
    }

    public async Task<ScenarioTestResult> RunScenarioAsync(string scenarioName, string token, string workstreamId)
    {
        var scenario = await GetScenarioDefinitionAsync(scenarioName, token, workstreamId);
        if (scenario == null)
        {
            return new ScenarioTestResult
            {
                ScenarioName = scenarioName,
                Description = $"Scenario '{scenarioName}' not found or user has no permissions for this resource.",
                Success = false,
                IsAuthorized = false,
                Decision = "Error",
                ErrorMessage = $"Scenario '{scenarioName}' not found or user has no permissions for this resource."
            };
        }

        var result = new ScenarioTestResult
        {
            ScenarioName = scenario.Name,
            Description = scenario.Description,
            TestResults = []
        };

        // Execute each test step in the scenario
        foreach (var step in scenario.Steps)
        {
            var testRequest = new AuthorizationTestRequest
            {
                Token = token,
                WorkstreamId = workstreamId,
                Resource = step.Resource,
                Action = step.Action,
                MockEntityJson = step.MockEntityJson,
                TestDescription = step.Description
            };

            _logger.LogInformation("Executing scenario step: {Description} - Resource: {Resource}, Action: {Action}, Expected: {Expected}",
                step.Description, step.Resource, step.Action, step.ExpectedResult);

            var stepResult = await _authorizationTestingService.CheckAuthorizationAsync(testRequest);
            stepResult.TestDescription = step.Description;

            _logger.LogInformation("Step result - IsAuthorized: {IsAuthorized}, Decision: {Decision}",
                stepResult.IsAuthorized, stepResult.Decision);

            // Check if result matches expected outcome
            if (step.ExpectedResult == "Allow" && !stepResult.IsAuthorized)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = $"Expected: Allow, Actual: Deny";
                _logger.LogWarning("Test FAILED: Expected Allow but got Deny for {Resource}/{Action}", step.Resource, step.Action);
            }
            else if (step.ExpectedResult == "Deny" && stepResult.IsAuthorized)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = $"Expected: Deny, Actual: Allow - This is a security issue!";
                _logger.LogWarning("Test FAILED: Expected Deny but got Allow for {Resource}/{Action} - SECURITY ISSUE!", step.Resource, step.Action);
            }
            else
            {
                _logger.LogInformation("Test PASSED: Result matches expected outcome");
            }

            result.TestResults.Add(stepResult);
        }

        result.TotalTests = result.TestResults.Count;
        result.PassedTests = result.TestResults.Count(t => t.Success &&
            ((t.IsAuthorized && scenario.Steps[result.TestResults.IndexOf(t)].ExpectedResult == "Allow") ||
             (!t.IsAuthorized && scenario.Steps[result.TestResults.IndexOf(t)].ExpectedResult == "Deny")));
        result.FailedTests = result.TotalTests - result.PassedTests;
        result.Success = result.FailedTests == 0;

        _logger.LogInformation("Scenario {ScenarioName} completed - Total: {Total}, Passed: {Passed}, Failed: {Failed}, Success: {Success}",
            scenario.Name, result.TotalTests, result.PassedTests, result.FailedTests, result.Success);

        // Set overall authorization result (for UI compatibility)
        result.IsAuthorized = result.Success;
        result.Decision = result.Success ? "Scenario Passed" : "Scenario Failed";

        // Combine all evaluation traces from test results
        foreach (var testResult in result.TestResults)
        {
            if (testResult.EvaluationTrace != null)
            {
                result.EvaluationTrace.AddRange(testResult.EvaluationTrace);
            }
        }

        return result;
    }

    private async Task<ScenarioDefinition?> GetScenarioDefinitionAsync(string scenarioName, string token, string workstreamId)
    {
        // Decode token to get user info
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
        var groups = jwtToken.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
        var roles = jwtToken.Claims.Where(c => c.Type == "roles" || c.Type == "role").Select(c => c.Value).ToList();

        // Build initial subjects for Casbin
        var subjects = new List<string>();
        if (!string.IsNullOrWhiteSpace(userId)) subjects.Add(userId);
        subjects.AddRange(roles.Where(r => !string.IsNullOrWhiteSpace(r)));
        subjects.AddRange(groups.Where(g => !string.IsNullOrWhiteSpace(g)));

        // Resolve group-to-role mappings (type "g" policies)
        var roleMappings = (await _policyRepository.GetBySubjectIdsAsync(subjects, policyType: "g"))
            .Where(p => p.WorkstreamId == workstreamId || p.WorkstreamId == "*")
            .ToList();

        var resolvedRoles = roleMappings
            .Where(g => !string.IsNullOrWhiteSpace(g.V1))
            .Select(g => g.V1!)
            .Distinct()
            .ToList();

        // Add resolved roles to subjects
        subjects.AddRange(resolvedRoles);

        // Get all permission policies for this workstream and resolved subjects
        var policies = (await _policyRepository.GetBySubjectIdsAsync(subjects, policyType: "p"))
            .Where(p => p.WorkstreamId == workstreamId || p.WorkstreamId == "*")
            .ToList();

        // Extract the resource from scenario name (format: "{workstream}-{base64Resource}")
        var parts = scenarioName.Split('-', 2);
        if (parts.Length < 2) return null;

        // Decode the Base64 encoded resource name
        string resource;
        try
        {
            var resourceBytes = Convert.FromBase64String(parts[1]);
            resource = System.Text.Encoding.UTF8.GetString(resourceBytes);
        }
        catch (FormatException)
        {
            // Fallback for old format (if any scenarios were created before the Base64 change)
            _logger.LogWarning("Failed to decode Base64 resource from scenario name '{ScenarioName}', trying legacy format", scenarioName);
            resource = parts[1].Replace("-", "/").Replace("id", ":id");
        }

        // Get all actions user can perform on this resource
        // V0=Subject, V1=Workstream, V2=Resource, V3=Action, V4=Effect
        var allowedActions = policies
            .Where(p => (p.V2 == resource || p.V2 == "*") && !string.IsNullOrWhiteSpace(p.V3))
            .Select(p => p.V3!)
            .Distinct()
            .ToList();

        if (allowedActions.Count == 0) return null;

        // Create scenario with steps for each allowed action
        var scenario = new ScenarioDefinition
        {
            Name = $"{resource} - Authorized Actions",
            Description = $"Test all authorized actions for {resource}",
            Steps = []
        };

        foreach (var action in allowedActions)
        {
            scenario.Steps.Add(new ScenarioStep
            {
                Description = $"{action} on {resource}",
                Resource = resource,
                Action = action,
                ExpectedResult = "Allow" // User has permission, so we expect Allow
            });
        }

        return scenario;
    }

    private class ScenarioDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ScenarioStep> Steps { get; set; } = [];
    }

    private class ScenarioStep
    {
        public string Description { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? MockEntityJson { get; set; }
        public string ExpectedResult { get; set; } = "Allow"; // "Allow" or "Deny"
    }
}
