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
    IAbacRuleRepository abacRuleRepository,
    IUserAttributeRepository userAttributeRepository,
    ILogger<ScenarioTestingService> logger) : IScenarioTestingService
{
    private readonly IPolicyRepository _policyRepository = policyRepository;
    private readonly IAuthorizationTestingService _authorizationTestingService = authorizationTestingService;
    private readonly IAbacRuleRepository _abacRuleRepository = abacRuleRepository;
    private readonly IUserAttributeRepository _userAttributeRepository = userAttributeRepository;
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

        // Get ABAC rules and user attributes for enhanced scenario generation
        Api.Modules.AccessControl.Persistence.Entities.Authorization.UserAttribute? userAttributes = null;
        IEnumerable<Api.Modules.AccessControl.Persistence.Entities.Authorization.AbacRule> abacRules = Enumerable.Empty<Api.Modules.AccessControl.Persistence.Entities.Authorization.AbacRule>();

        if (!string.IsNullOrEmpty(userId))
        {
            userAttributes = await _userAttributeRepository.GetByUserIdAndWorkstreamAsync(userId, workstreamId);
            abacRules = await _abacRuleRepository.SearchAsync(workstreamId);
            _logger.LogInformation("Found {Count} ABAC rules for workstream {Workstream}", abacRules.Count(), workstreamId);
        }
        else
        {
            _logger.LogWarning("Cannot generate ABAC scenarios without userId - will only generate RBAC scenarios");
        }

        var hasAbacRules = abacRules.Any();

        // Group by resource to create scenarios
        var resourceGroups = resourceActions.GroupBy(ra => ra.Resource);

        foreach (var resourceGroup in resourceGroups)
        {
            var resource = resourceGroup.Key;
            var actions = resourceGroup.Select(ra => ra.Action).ToList();

            // Create a basic scenario for this resource showing what actions are allowed (without ABAC)
            // Use Base64 encoding for the resource to preserve exact casing and special characters
            var resourceId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(resource));
            var scenario = new DynamicScenario
            {
                Id = $"{workstreamId}-{resourceId}",
                Name = $"{resource} - Authorized Actions (RBAC Only)",
                Description = $"Test basic role-based permissions for {resource} in {workstreamId} workstream (no entity data)",
                Resource = resource,
                AvailableActions = actions,
                WorkstreamId = workstreamId
            };

            scenarios.Add(scenario);

            // Generate ABAC-aware scenarios if there are ABAC rules and user attributes
            if (hasAbacRules && userAttributes != null && actions.Any())
            {
                var abacScenarios = await GenerateAbacScenarios(resource, actions, userAttributes, workstreamId, abacRules);
                scenarios.AddRange(abacScenarios);
            }
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
        // First, check if this is a predefined ABAC scenario by getting all available scenarios
        var allScenarios = await GetAvailableScenariosAsync(token, workstreamId);
        var matchingScenario = allScenarios.FirstOrDefault(s => s.Id == scenarioName);

        if (matchingScenario != null)
        {
            // This is a predefined scenario (either RBAC-only or ABAC-aware)
            var scenario = new ScenarioDefinition
            {
                Name = matchingScenario.Name,
                Description = matchingScenario.Description,
                Steps = []
            };

            // Determine expected result based on scenario description
            string expectedResult = "Allow";
            if (matchingScenario.Description.Contains("should FAIL", StringComparison.OrdinalIgnoreCase) ||
                matchingScenario.Description.Contains("should fail", StringComparison.OrdinalIgnoreCase) ||
                matchingScenario.Description.Contains("should deny", StringComparison.OrdinalIgnoreCase))
            {
                expectedResult = "Deny";
            }

            // Create steps for each available action in the scenario
            foreach (var action in matchingScenario.AvailableActions)
            {
                scenario.Steps.Add(new ScenarioStep
                {
                    Description = $"{action} on {matchingScenario.Resource}" +
                        (matchingScenario.MockEntityJson != null ? " (with ABAC evaluation)" : ""),
                    Resource = matchingScenario.Resource,
                    Action = action,
                    MockEntityJson = matchingScenario.MockEntityJson,
                    ExpectedResult = expectedResult
                });
            }

            return scenario;
        }

        // Fallback: scenario not found in available scenarios
        return null;
    }

    /// <summary>
    /// Generates ABAC-aware test scenarios dynamically based on actual ABAC rules configuration.
    /// Parses rule configurations to understand what attributes and comparisons are being tested.
    /// </summary>
    private async Task<List<DynamicScenario>> GenerateAbacScenarios(
        string resource,
        List<string> actions,
        Api.Modules.AccessControl.Persistence.Entities.Authorization.UserAttribute userAttrs,
        string workstreamId,
        IEnumerable<Api.Modules.AccessControl.Persistence.Entities.Authorization.AbacRule> abacRules)
    {
        var scenarios = new List<DynamicScenario>();

        // Parse user attributes from JSON
        var userAttributesDict = !string.IsNullOrEmpty(userAttrs.AttributesJson)
            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(userAttrs.AttributesJson)
            : null;

        if (userAttributesDict == null || !abacRules.Any())
        {
            return scenarios; // No attributes or rules to test
        }

        // Only generate ABAC scenarios for instance-level operations (resources with wildcards)
        if (!resource.Contains("/*") && !resource.Contains("/:"))
        {
            return scenarios;
        }

        // Parse ABAC rules to understand what they're testing
        var ruleAnalysis = AnalyzeAbacRules(abacRules, userAttributesDict);

        // Get all actions from policies to determine which ones might trigger ABAC evaluation
        // Query Casbin policies with PolicyType='p' and extract actions from V3 column
        var allPolicies = await _policyRepository.SearchAsync(workstreamId, policyType: "p");
        var actionsWithAbacRules = allPolicies
            .Where(p => !string.IsNullOrEmpty(p.V3) && abacRules.Any())
            .Select(p => p.V3!)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Filter to only actions that exist in both the available actions list and have policies defined
        var abacAwareActions = actions.Where(a => actionsWithAbacRules.Contains(a)).ToList();

        foreach (var action in abacAwareActions)
        {
            // Generate test scenarios based on what the rules are actually testing
            scenarios.AddRange(await GenerateScenariosForRuleAnalysis(resource, action, workstreamId, ruleAnalysis));
        }

        return scenarios;
    }

    /// <summary>
    /// Analyzes ABAC rules to understand what attributes they test and how.
    /// Returns structured information about comparisons, matches, and ranges.
    /// </summary>
    private AbacRuleAnalysis AnalyzeAbacRules(
        IEnumerable<Api.Modules.AccessControl.Persistence.Entities.Authorization.AbacRule> abacRules,
        Dictionary<string, System.Text.Json.JsonElement> userAttributes)
    {
        var analysis = new AbacRuleAnalysis();

        foreach (var rule in abacRules.Where(r => r.IsActive))
        {
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(rule.Configuration);
                if (config == null) continue;

                var ruleType = config.ContainsKey("ruleType") ? config["ruleType"].GetString() : rule.RuleType;

                switch (ruleType)
                {
                    case "AttributeComparison":
                        // Example: {"userAttribute": "ApprovalLimit", "operator": ">=", "resourceProperty": "Amount"}
                        if (config.ContainsKey("userAttribute") && config.ContainsKey("resourceProperty"))
                        {
                            var userAttr = config["userAttribute"].GetString();
                            var resourceProp = config["resourceProperty"].GetString();
                            var op = config.ContainsKey("operator") ? config["operator"].GetString() : ">=";

                            if (!string.IsNullOrEmpty(userAttr) && !string.IsNullOrEmpty(resourceProp) && userAttributes.ContainsKey(userAttr))
                            {
                                analysis.AttributeComparisons.Add(new AttributeComparison
                                {
                                    UserAttribute = userAttr,
                                    ResourceProperty = resourceProp,
                                    Operator = op ?? ">=",
                                    UserValue = userAttributes[userAttr]
                                });
                            }
                        }
                        break;

                    case "PropertyMatch":
                        // Example: {"userAttribute": "Region", "resourceProperty": "Region"}
                        if (config.ContainsKey("userAttribute") && config.ContainsKey("resourceProperty"))
                        {
                            var userAttr = config["userAttribute"].GetString();
                            var resourceProp = config["resourceProperty"].GetString();

                            if (!string.IsNullOrEmpty(userAttr) && !string.IsNullOrEmpty(resourceProp) && userAttributes.ContainsKey(userAttr))
                            {
                                analysis.PropertyMatches.Add(new PropertyMatch
                                {
                                    UserAttribute = userAttr,
                                    ResourceProperty = resourceProp,
                                    UserValue = userAttributes[userAttr]
                                });
                            }
                        }
                        break;

                    case "ValueRange":
                        // Example: {"attributeName": "Amount", "min": 1000, "max": 50000}
                        if (config.ContainsKey("attributeName"))
                        {
                            var attrName = config["attributeName"].GetString();
                            if (!string.IsNullOrEmpty(attrName))
                            {
                                decimal? min = config.ContainsKey("min") ? config["min"].GetDecimal() : null;
                                decimal? max = config.ContainsKey("max") ? config["max"].GetDecimal() : null;

                                analysis.ValueRanges.Add(new ValueRange
                                {
                                    AttributeName = attrName,
                                    Min = min,
                                    Max = max
                                });
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ABAC rule configuration for rule {RuleId}", rule.Id);
            }
        }

        return analysis;
    }

    /// <summary>
    /// Generates test scenarios based on analyzed ABAC rules.
    /// Creates both passing and failing scenarios to test rule boundaries.
    /// </summary>
    private async Task<List<DynamicScenario>> GenerateScenariosForRuleAnalysis(
        string resource,
        string action,
        string workstreamId,
        AbacRuleAnalysis analysis)
    {
        var scenarios = new List<DynamicScenario>();
        var mockEntity = new Dictionary<string, object>();

        // Scenario 1: Generate a PASSING scenario with all rules satisfied
        bool canGeneratePassingScenario = true;

        // Add values that satisfy AttributeComparisons (numeric comparisons)
        foreach (var comparison in analysis.AttributeComparisons)
        {
            try
            {
                var userValue = comparison.UserValue.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? comparison.UserValue.GetDecimal()
                    : (decimal?)null;

                if (!userValue.HasValue)
                {
                    canGeneratePassingScenario = false;
                    continue;
                }

                // For ">=" operator, use 75% of user's limit (well within bounds)
                // For other operators, adjust accordingly
                decimal resourceValue = comparison.Operator switch
                {
                    ">=" or ">" => Math.Floor(userValue.Value * 0.75m),
                    "<=" or "<" => Math.Ceiling(userValue.Value * 1.25m),
                    "==" => userValue.Value,
                    _ => Math.Floor(userValue.Value * 0.75m)
                };

                mockEntity[comparison.ResourceProperty] = resourceValue;
            }
            catch
            {
                canGeneratePassingScenario = false;
            }
        }

        // Add values that satisfy PropertyMatches (exact matches)
        foreach (var match in analysis.PropertyMatches)
        {
            try
            {
                var userValue = match.UserValue.ValueKind == System.Text.Json.JsonValueKind.String
                    ? match.UserValue.GetString()
                    : match.UserValue.ToString();

                if (!string.IsNullOrEmpty(userValue))
                {
                    mockEntity[match.ResourceProperty] = userValue;
                }
                else
                {
                    canGeneratePassingScenario = false;
                }
            }
            catch
            {
                canGeneratePassingScenario = false;
            }
        }

        // Add values within ValueRanges
        foreach (var range in analysis.ValueRanges)
        {
            if (range.Min.HasValue && range.Max.HasValue)
            {
                // Use midpoint of range
                mockEntity[range.AttributeName] = (range.Min.Value + range.Max.Value) / 2;
            }
            else if (range.Min.HasValue)
            {
                mockEntity[range.AttributeName] = range.Min.Value * 1.5m;
            }
            else if (range.Max.HasValue)
            {
                mockEntity[range.AttributeName] = range.Max.Value * 0.5m;
            }
        }

        // Generate passing scenario if we have enough data
        if (canGeneratePassingScenario && mockEntity.Any())
        {
            var scenarioId = $"{workstreamId}-{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{resource}-{action}-abac-pass"))}";
            scenarios.Add(new DynamicScenario
            {
                Id = scenarioId,
                Name = $"{resource} - {action} (ABAC Pass)",
                Description = $"Test {action} with attributes that satisfy all ABAC rules - should PASS",
                Resource = resource,
                AvailableActions = [action],
                WorkstreamId = workstreamId,
                MockEntityJson = System.Text.Json.JsonSerializer.Serialize(mockEntity)
            });
        }

        // Scenario 2: Generate FAILING scenarios for each rule type
        // Test AttributeComparison failures (deduplicated by UserAttribute)
        var processedAttributeComparisons = new HashSet<string>();
        foreach (var comparison in analysis.AttributeComparisons)
        {
            try
            {
                // Skip if we've already created a scenario for this attribute
                if (processedAttributeComparisons.Contains(comparison.UserAttribute))
                    continue;

                var userValue = comparison.UserValue.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? comparison.UserValue.GetDecimal()
                    : (decimal?)null;

                if (!userValue.HasValue) continue;

                var failingMockEntity = new Dictionary<string, object>(mockEntity);

                // Create a value that will FAIL the comparison
                decimal failingValue = comparison.Operator switch
                {
                    ">=" or ">" => Math.Ceiling(userValue.Value * 1.5m), // Exceeds limit
                    "<=" or "<" => Math.Floor(userValue.Value * 0.5m),   // Below limit
                    "==" => userValue.Value + 1,                          // Not equal
                    _ => Math.Ceiling(userValue.Value * 1.5m)
                };

                failingMockEntity[comparison.ResourceProperty] = failingValue;

                var scenarioId = $"{workstreamId}-{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{resource}-{action}-fail-{comparison.UserAttribute}"))}";
                var displayValue = comparison.UserValue.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? $"${userValue:N0}"
                    : comparison.UserValue.ToString();

                scenarios.Add(new DynamicScenario
                {
                    Id = scenarioId,
                    Name = $"{resource} - {action} (Fails {comparison.UserAttribute} Check)",
                    Description = $"Test {action} with {comparison.ResourceProperty}=${failingValue:N0} - should FAIL ABAC (user {comparison.UserAttribute} is {displayValue})",
                    Resource = resource,
                    AvailableActions = [action],
                    WorkstreamId = workstreamId,
                    MockEntityJson = System.Text.Json.JsonSerializer.Serialize(failingMockEntity)
                });

                processedAttributeComparisons.Add(comparison.UserAttribute);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate failing scenario for AttributeComparison {UserAttribute}", comparison.UserAttribute);
            }
        }

        // Test PropertyMatch failures (deduplicated by UserAttribute)
        var processedPropertyMatches = new HashSet<string>();
        foreach (var match in analysis.PropertyMatches)
        {
            try
            {
                // Skip if we've already created a scenario for this attribute
                if (processedPropertyMatches.Contains(match.UserAttribute))
                    continue;

                var userValue = match.UserValue.ValueKind == System.Text.Json.JsonValueKind.String
                    ? match.UserValue.GetString()
                    : match.UserValue.ToString();

                if (string.IsNullOrEmpty(userValue)) continue;

                var failingMockEntity = new Dictionary<string, object>(mockEntity);

                // Get a mismatched value dynamically from other users' attribute values in the database
                string mismatchedValue = await GetAlternativeAttributeValueAsync(match.UserAttribute, userValue, workstreamId);

                failingMockEntity[match.ResourceProperty] = mismatchedValue;

                var scenarioId = $"{workstreamId}-{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{resource}-{action}-mismatch-{match.UserAttribute}"))}";

                scenarios.Add(new DynamicScenario
                {
                    Id = scenarioId,
                    Name = $"{resource} - {action} (Mismatched {match.UserAttribute})",
                    Description = $"Test {action} with {match.ResourceProperty}={mismatchedValue} - should FAIL ABAC (user {match.UserAttribute} is {userValue})",
                    Resource = resource,
                    AvailableActions = [action],
                    WorkstreamId = workstreamId,
                    MockEntityJson = System.Text.Json.JsonSerializer.Serialize(failingMockEntity)
                });

                processedPropertyMatches.Add(match.UserAttribute);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate failing scenario for PropertyMatch {UserAttribute}", match.UserAttribute);
            }
        }

        return scenarios;
    }

    /// <summary>
    /// Gets an alternative attribute value from the database for testing mismatches.
    /// Queries other users' attributes to find a different value for the same attribute.
    /// </summary>
    private async Task<string> GetAlternativeAttributeValueAsync(string attributeName, string currentValue, string workstreamId)
    {
        try
        {
            // Query all user attributes for this workstream
            var allUserAttributes = await _userAttributeRepository.SearchAsync(workstreamId);

            // Parse all attributes and find different values for the specified attribute
            var alternativeValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var userAttr in allUserAttributes)
            {
                if (string.IsNullOrEmpty(userAttr.AttributesJson)) continue;

                try
                {
                    var attrDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(userAttr.AttributesJson);
                    if (attrDict != null && attrDict.ContainsKey(attributeName))
                    {
                        var value = attrDict[attributeName].ValueKind == System.Text.Json.JsonValueKind.String
                            ? attrDict[attributeName].GetString()
                            : attrDict[attributeName].ToString();

                        if (!string.IsNullOrEmpty(value) && !value.Equals(currentValue, StringComparison.OrdinalIgnoreCase))
                        {
                            alternativeValues.Add(value);
                        }
                    }
                }
                catch
                {
                    // Skip invalid JSON
                    continue;
                }
            }

            // Return first alternative value found, or generate a generic one
            return alternativeValues.FirstOrDefault() ?? $"Alternative-{attributeName}-Value";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get alternative attribute value for {AttributeName}", attributeName);
            return $"Alternative-{attributeName}-Value";
        }
    }

    // Helper classes for rule analysis
    private class AbacRuleAnalysis
    {
        public List<AttributeComparison> AttributeComparisons { get; set; } = new();
        public List<PropertyMatch> PropertyMatches { get; set; } = new();
        public List<ValueRange> ValueRanges { get; set; } = new();
    }

    private class AttributeComparison
    {
        public required string UserAttribute { get; set; }
        public required string ResourceProperty { get; set; }
        public required string Operator { get; set; }
        public required System.Text.Json.JsonElement UserValue { get; set; }
    }

    private class PropertyMatch
    {
        public required string UserAttribute { get; set; }
        public required string ResourceProperty { get; set; }
        public required System.Text.Json.JsonElement UserValue { get; set; }
    }

    private class ValueRange
    {
        public required string AttributeName { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
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
