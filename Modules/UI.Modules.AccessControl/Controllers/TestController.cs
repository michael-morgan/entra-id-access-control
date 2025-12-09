using Api.Modules.AccessControl.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using UI.Modules.AccessControl.Models;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for testing authorization with JWT tokens and viewing all associated access control data.
/// </summary>
public class TestController(AccessControlDbContext context, ILogger<TestController> logger) : Controller
{
    private readonly AccessControlDbContext _context = context;
    private readonly ILogger<TestController> _logger = logger;

    // GET: Test
    public IActionResult Index()
    {
        return View();
    }

    // POST: Test/DecodeToken
    [HttpPost]
    public async Task<IActionResult> DecodeToken([FromBody] DecodeTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Json(new TokenAnalysisResult
            {
                Success = false,
                ErrorMessage = "Token is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.WorkstreamId))
        {
            return Json(new TokenAnalysisResult
            {
                Success = false,
                ErrorMessage = "Workstream ID is required."
            });
        }

        try
        {
            // Decode JWT token without validation (display-only)
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(request.Token);

            var result = new TokenAnalysisResult
            {
                Success = true
            };

            // Extract all claims
            foreach (var claim in jwtToken.Claims)
            {
                result.Claims[claim.Type] = claim.Value;
            }

            // Extract specific claims
            result.UserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            result.UserName = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            result.Email = jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username" || c.Type == "email")?.Value;

            // Extract groups (may be multiple claims with type "groups")
            result.Groups = [.. jwtToken.Claims
                .Where(c => c.Type == "groups")
                .Select(c => c.Value)];

            // Extract roles (may be multiple claims with type "roles")
            result.Roles = [.. jwtToken.Claims
                .Where(c => c.Type == "roles" || c.Type == "role")
                .Select(c => c.Value)];

            // Get ABAC attributes for the user (stored as JSON)
            if (!string.IsNullOrWhiteSpace(result.UserId))
            {
                var userAttrs = await _context.UserAttributes
                    .Where(ua => ua.UserId == result.UserId && ua.WorkstreamId == request.WorkstreamId)
                    .FirstOrDefaultAsync();

                if (userAttrs != null && !string.IsNullOrWhiteSpace(userAttrs.AttributesJson))
                {
                    try
                    {
                        var attrs = JsonSerializer.Deserialize<Dictionary<string, object>>(userAttrs.AttributesJson);
                        if (attrs != null)
                        {
                            foreach (var (key, value) in attrs)
                            {
                                result.UserAttributes.Add(new AttributeViewModel
                                {
                                    AttributeName = key,
                                    Value = value?.ToString() ?? "",
                                    Source = "User",
                                    EntityId = userAttrs.UserId
                                });
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse user attributes JSON");
                    }
                }
            }

            // Get role attributes for all roles (stored as JSON)
            if (result.Roles.Count != 0)
            {
                var roleAttrs = await _context.RoleAttributes
                    .Where(ra => result.Roles.Contains(ra.RoleValue) && ra.WorkstreamId == request.WorkstreamId)
                    .ToListAsync();

                foreach (var roleAttr in roleAttrs)
                {
                    if (!string.IsNullOrWhiteSpace(roleAttr.AttributesJson))
                    {
                        try
                        {
                            var attrs = JsonSerializer.Deserialize<Dictionary<string, object>>(roleAttr.AttributesJson);
                            if (attrs != null)
                            {
                                foreach (var (key, value) in attrs)
                                {
                                    result.RoleAttributes.Add(new AttributeViewModel
                                    {
                                        AttributeName = key,
                                        Value = value?.ToString() ?? "",
                                        Source = "Role",
                                        EntityId = roleAttr.RoleValue
                                    });
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse role attributes JSON for role {RoleValue}", roleAttr.RoleValue);
                        }
                    }
                }
            }

            // Get group attributes for all groups (stored as JSON)
            if (result.Groups.Any())
            {
                var groupAttrs = await _context.GroupAttributes
                    .Where(ga => result.Groups.Contains(ga.GroupId) && ga.WorkstreamId == request.WorkstreamId)
                    .ToListAsync();

                foreach (var groupAttr in groupAttrs)
                {
                    if (!string.IsNullOrWhiteSpace(groupAttr.AttributesJson))
                    {
                        try
                        {
                            var attrs = JsonSerializer.Deserialize<Dictionary<string, object>>(groupAttr.AttributesJson);
                            if (attrs != null)
                            {
                                foreach (var (key, value) in attrs)
                                {
                                    result.GroupAttributes.Add(new AttributeViewModel
                                    {
                                        AttributeName = key,
                                        Value = value?.ToString() ?? "",
                                        Source = "Group",
                                        EntityId = groupAttr.GroupId
                                    });
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse group attributes JSON for group {GroupId}", groupAttr.GroupId);
                        }
                    }
                }
            }

            // Merge attributes with precedence: User > Role > Group
            var mergedAttributes = new Dictionary<string, AttributeValue>();

            // Start with group attributes (lowest precedence)
            foreach (var attr in result.GroupAttributes)
            {
                mergedAttributes[attr.AttributeName] = new AttributeValue
                {
                    Value = attr.Value,
                    Source = attr.Source,
                    EntityId = attr.EntityId
                };
            }

            // Override with role attributes
            foreach (var attr in result.RoleAttributes)
            {
                mergedAttributes[attr.AttributeName] = new AttributeValue
                {
                    Value = attr.Value,
                    Source = attr.Source,
                    EntityId = attr.EntityId
                };
            }

            // Override with user attributes (highest precedence)
            foreach (var attr in result.UserAttributes)
            {
                mergedAttributes[attr.AttributeName] = new AttributeValue
                {
                    Value = attr.Value,
                    Source = attr.Source,
                    EntityId = attr.EntityId
                };
            }

            result.MergedAttributes = mergedAttributes;

            // Get applicable Casbin policies
            var subjects = new List<string>();
            if (!string.IsNullOrWhiteSpace(result.UserId))
                subjects.Add(result.UserId);
            subjects.AddRange(result.Roles.Where(r => !string.IsNullOrWhiteSpace(r)));
            subjects.AddRange(result.Groups.Where(g => !string.IsNullOrWhiteSpace(g)));

            var policies = await _context.CasbinPolicies
                .Where(p => (p.WorkstreamId == request.WorkstreamId || p.WorkstreamId == "*") &&
                           (subjects.Contains(p.V0!) || (p.V1 != null && subjects.Contains(p.V1))))
                .OrderBy(p => p.PolicyType)
                .ThenBy(p => p.V0)
                .ToListAsync();

            result.ApplicablePolicies = [.. policies.Select(p => new PolicySummary
            {
                Id = p.Id,
                PolicyType = p.PolicyType,
                V0 = p.V0 ?? "",
                V1 = p.V1,
                V2 = p.V2,
                V3 = p.V3,
                V4 = p.V4,
                V5 = p.V5,
                WorkstreamId = p.WorkstreamId ?? "",
                DisplayText = FormatPolicyDisplay(p)
            })];

            // Get ABAC rules for the workstream
            var abacRules = await _context.AbacRules
                .Where(r => r.WorkstreamId == request.WorkstreamId && r.IsActive)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.RuleName)
                .ToListAsync();

            result.ApplicableAbacRules = [.. abacRules.Select(r => new AbacRuleSummary
            {
                Id = r.Id,
                RuleType = r.RuleType,
                AttributeName = r.RuleName,
                Operator = "See Configuration",
                ComparisonValue = null,
                EntityAttribute = null,
                WorkstreamId = r.WorkstreamId,
                DisplayText = FormatAbacRuleDisplay(r)
            })];

            // Get ABAC rule groups for the workstream
            var ruleGroups = await _context.AbacRuleGroups
                .Include(rg => rg.Rules)
                .Where(rg => rg.WorkstreamId == request.WorkstreamId && rg.IsActive)
                .OrderBy(rg => rg.Priority)
                .ThenBy(rg => rg.GroupName)
                .ToListAsync();

            result.ApplicableRuleGroups = [.. ruleGroups.Select(rg => new AbacRuleGroupSummary
            {
                Id = rg.Id,
                GroupName = rg.GroupName,
                LogicOperator = rg.LogicalOperator,
                RuleCount = rg.Rules?.Count ?? 0,
                WorkstreamId = rg.WorkstreamId
            })];

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding token");
            return Json(new TokenAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Error decoding token: {ex.Message}"
            });
        }
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
    public async Task<IActionResult> CheckAuthorization([FromBody] AuthorizationTestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Json(new AuthorizationTestResult
            {
                Success = false,
                ErrorMessage = "Token is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.WorkstreamId) || string.IsNullOrWhiteSpace(request.Resource) || string.IsNullOrWhiteSpace(request.Action))
        {
            return Json(new AuthorizationTestResult
            {
                Success = false,
                ErrorMessage = "Workstream, Resource, and Action are required."
            });
        }

        try
        {
            // Step 1: Decode JWT token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(request.Token);

            var result = new AuthorizationTestResult
            {
                Success = true,
                TestDescription = request.TestDescription ?? $"Test {request.Action} on {request.Resource}",
                Resource = request.Resource,
                Action = request.Action,
                WorkstreamId = request.WorkstreamId,
                MockEntityJson = request.MockEntityJson
            };

            var trace = new List<AuthorizationStep>();
            var stepOrder = 0;

            // Step 2: Extract user identity
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
            var userName = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

            trace.Add(new AuthorizationStep
            {
                StepName = "JWT Claims Extracted",
                Description = $"User ID: {userId}, Name: {userName}",
                Result = "Pass",
                Details = $"Token contains {jwtToken.Claims.Count()} claims",
                Order = stepOrder++
            });

            // Step 3: Get groups and roles
            var groups = jwtToken.Claims
                .Where(c => c.Type == "groups")
                .Select(c => c.Value)
                .ToList();

            var roles = jwtToken.Claims
                .Where(c => c.Type == "roles" || c.Type == "role")
                .Select(c => c.Value)
                .ToList();

            trace.Add(new AuthorizationStep
            {
                StepName = "Groups and Roles Identified",
                Description = $"Groups: [{string.Join(", ", groups)}], Roles: [{string.Join(", ", roles)}]",
                Result = "Pass",
                Details = $"Found {groups.Count} groups and {roles.Count} roles",
                Order = stepOrder++
            });

            // Step 4: Build subjects list for Casbin
            var subjects = new List<string>();
            if (!string.IsNullOrWhiteSpace(userId))
                subjects.Add(userId);
            subjects.AddRange(roles.Where(r => !string.IsNullOrWhiteSpace(r)));
            subjects.AddRange(groups.Where(g => !string.IsNullOrWhiteSpace(g)));

            // Step 4.5: Resolve group-to-role mappings (type "g" policies)
            var roleMappings = await _context.CasbinPolicies
                .Where(p => (p.WorkstreamId == request.WorkstreamId || p.WorkstreamId == "*") &&
                           p.PolicyType == "g" &&
                           subjects.Contains(p.V0!))
                .ToListAsync();

            var resolvedRoles = roleMappings
                .Where(g => !string.IsNullOrWhiteSpace(g.V1))
                .Select(g => g.V1!)
                .Distinct()
                .ToList();

            subjects.AddRange(resolvedRoles);

            trace.Add(new AuthorizationStep
            {
                StepName = "Role Resolution",
                Description = $"Resolved roles: [{string.Join(", ", resolvedRoles)}]",
                Result = "Pass",
                Details = $"Found {roleMappings.Count} group-to-role mappings, resolved to {resolvedRoles.Count} roles",
                Order = stepOrder++
            });

            // Step 5: Check RBAC policies (Casbin)
            // V0=Subject, V1=Workstream, V2=Resource, V3=Action, V4=Effect
            var policies = await _context.CasbinPolicies
                .Where(p => (p.WorkstreamId == request.WorkstreamId || p.WorkstreamId == "*") &&
                           p.PolicyType == "p" &&
                           subjects.Contains(p.V0!))
                .ToListAsync();

            var matchingPermissions = policies
                .Where(p => p.PolicyType == "p" &&
                           subjects.Contains(p.V0 ?? "") &&
                           (p.V2 == request.Resource || p.V2 == "*") &&
                           (p.V3 == request.Action || p.V3 == "*"))
                .ToList();

            bool rbacAllowed = matchingPermissions.Count != 0;

            trace.Add(new AuthorizationStep
            {
                StepName = "RBAC Check (Casbin)",
                Description = rbacAllowed
                    ? $"Found matching policy: {FormatPolicyDisplay(matchingPermissions.First())}"
                    : "No matching RBAC policy found",
                Result = rbacAllowed ? "Pass" : "Fail",
                Details = $"Evaluated {policies.Count} policies, {matchingPermissions.Count} matches",
                Order = stepOrder++
            });

            // Step 6: Check ABAC rules (if entity provided)
            bool abacAllowed = true;
            if (!string.IsNullOrWhiteSpace(request.MockEntityJson))
            {
                var abacRules = await _context.AbacRules
                    .Where(r => r.WorkstreamId == request.WorkstreamId && r.IsActive)
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                // Simulate ABAC evaluation (simplified)
                trace.Add(new AuthorizationStep
                {
                    StepName = "ABAC Evaluation",
                    Description = $"Evaluated {abacRules.Count} ABAC rules against mock entity",
                    Result = "Pass",
                    Details = "ABAC evaluation requires full context - simulation mode",
                    Order = stepOrder++
                });
            }
            else
            {
                trace.Add(new AuthorizationStep
                {
                    StepName = "ABAC Evaluation",
                    Description = "No entity provided - ABAC rules skipped",
                    Result = "Skip",
                    Order = stepOrder++
                });
            }

            // Final decision
            result.IsAuthorized = rbacAllowed && abacAllowed;
            result.Decision = result.IsAuthorized ? "Allowed" : "Denied";

            trace.Add(new AuthorizationStep
            {
                StepName = "Final Decision",
                Description = result.IsAuthorized
                    ? $"Access to {request.Action} on {request.Resource} is ALLOWED"
                    : $"Access to {request.Action} on {request.Resource} is DENIED",
                Result = result.IsAuthorized ? "Pass" : "Fail",
                Details = result.IsAuthorized
                    ? "User has required permissions"
                    : "User lacks required permissions or ABAC constraints failed",
                Order = stepOrder++
            });

            result.EvaluationTrace = trace;

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authorization");
            return Json(new AuthorizationTestResult
            {
                Success = false,
                ErrorMessage = $"Error checking authorization: {ex.Message}",
                TestDescription = request.TestDescription ?? "",
                Resource = request.Resource,
                Action = request.Action,
                WorkstreamId = request.WorkstreamId
            });
        }
    }

    // POST: Test/RunScenario
    [HttpPost]
    public async Task<IActionResult> RunScenario([FromBody] RunScenarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.ScenarioName))
        {
            return Json(new ScenarioTestResult
            {
                ScenarioName = request.ScenarioName,
                Description = "Token and scenario name are required.",
                Success = false,
                IsAuthorized = false,
                Decision = "Error",
                ErrorMessage = "Token and scenario name are required."
            });
        }

        try
        {
            var scenario = await GetScenarioDefinitionAsync(request.ScenarioName, request.Token, request.WorkstreamId);
            if (scenario == null)
            {
                return Json(new ScenarioTestResult
                {
                    ScenarioName = request.ScenarioName,
                    Description = $"Scenario '{request.ScenarioName}' not found or user has no permissions for this resource.",
                    Success = false,
                    IsAuthorized = false,
                    Decision = "Error",
                    ErrorMessage = $"Scenario '{request.ScenarioName}' not found or user has no permissions for this resource."
                });
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
                    Token = request.Token,
                    WorkstreamId = request.WorkstreamId,
                    Resource = step.Resource,
                    Action = step.Action,
                    MockEntityJson = step.MockEntityJson,
                    TestDescription = step.Description
                };

                _logger.LogInformation("Executing scenario step: {Description} - Resource: {Resource}, Action: {Action}, Expected: {Expected}",
                    step.Description, step.Resource, step.Action, step.ExpectedResult);

                var stepResult = await CheckAuthorizationInternal(testRequest);
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

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running scenario {ScenarioName}", request.ScenarioName);
            return Json(new ScenarioTestResult
            {
                ScenarioName = request.ScenarioName,
                Description = $"Error running scenario: {ex.Message}",
                Success = false,
                IsAuthorized = false,
                Decision = "Error",
                ErrorMessage = ex.Message
            });
        }
    }

    private async Task<AuthorizationTestResult> CheckAuthorizationInternal(AuthorizationTestRequest request)
    {
        // Reuse the CheckAuthorization logic
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(request.Token);

        var result = new AuthorizationTestResult
        {
            Success = true,
            TestDescription = request.TestDescription ?? $"Test {request.Action} on {request.Resource}",
            Resource = request.Resource,
            Action = request.Action,
            WorkstreamId = request.WorkstreamId,
            MockEntityJson = request.MockEntityJson
        };

        // Extract user info
        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
        var groups = jwtToken.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
        var roles = jwtToken.Claims.Where(c => c.Type == "roles" || c.Type == "role").Select(c => c.Value).ToList();

        // Build initial subjects for Casbin
        var subjects = new List<string>();
        if (!string.IsNullOrWhiteSpace(userId)) subjects.Add(userId);
        subjects.AddRange(roles.Where(r => !string.IsNullOrWhiteSpace(r)));
        subjects.AddRange(groups.Where(g => !string.IsNullOrWhiteSpace(g)));

        // Resolve group-to-role mappings (type "g" policies)
        var roleMappings = await _context.CasbinPolicies
            .Where(p => (p.WorkstreamId == request.WorkstreamId || p.WorkstreamId == "*") &&
                       p.PolicyType == "g" &&
                       subjects.Contains(p.V0!))
            .ToListAsync();

        var resolvedRoles = roleMappings
            .Where(g => !string.IsNullOrWhiteSpace(g.V1))
            .Select(g => g.V1!)
            .Distinct()
            .ToList();

        // Add resolved roles to subjects
        subjects.AddRange(resolvedRoles);

        // Check RBAC
        var policies = await _context.CasbinPolicies
            .Where(p => (p.WorkstreamId == request.WorkstreamId || p.WorkstreamId == "*") &&
                       (subjects.Contains(p.V0!) || (p.V1 != null && subjects.Contains(p.V1))))
            .ToListAsync();

        // V0=Subject, V1=Workstream, V2=Resource, V3=Action, V4=Effect
        var matchingPermissions = policies
            .Where(p => p.PolicyType == "p" &&
                       subjects.Contains(p.V0 ?? "") &&
                       (p.V2 == request.Resource || p.V2 == "*") &&
                       (p.V3 == request.Action || p.V3 == "*"))
            .ToList();

        result.IsAuthorized = matchingPermissions.Count != 0;
        result.Decision = result.IsAuthorized ? "Allowed" : "Denied";
        result.EvaluationTrace = [];

        return result;
    }

    // POST: Test/GetAvailableScenarios
    [HttpPost]
    public async Task<IActionResult> GetAvailableScenarios([FromBody] GetScenariosRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.WorkstreamId))
        {
            return Json(new { success = false, errorMessage = "Token and workstream ID are required." });
        }

        try
        {
            var scenarios = await GenerateDynamicScenariosAsync(request.Token, request.WorkstreamId);
            return Json(new { success = true, scenarios });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating scenarios for workstream {WorkstreamId}", request.WorkstreamId);
            return Json(new { success = false, errorMessage = $"Error generating scenarios: {ex.Message}" });
        }
    }

    private async Task<List<DynamicScenario>> GenerateDynamicScenariosAsync(string token, string workstreamId)
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
        var roleMappings = await _context.CasbinPolicies
            .Where(p => (p.WorkstreamId == workstreamId || p.WorkstreamId == "*") &&
                       p.PolicyType == "g" &&
                       subjects.Contains(p.V0!))
            .ToListAsync();

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
        var policies = await _context.CasbinPolicies
            .Where(p => (p.WorkstreamId == workstreamId || p.WorkstreamId == "*") &&
                       p.PolicyType == "p" &&
                       subjects.Contains(p.V0!))
            .ToListAsync();

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
        var roleMappings = await _context.CasbinPolicies
            .Where(p => (p.WorkstreamId == workstreamId || p.WorkstreamId == "*") &&
                       p.PolicyType == "g" &&
                       subjects.Contains(p.V0!))
            .ToListAsync();

        var resolvedRoles = roleMappings
            .Where(g => !string.IsNullOrWhiteSpace(g.V1))
            .Select(g => g.V1!)
            .Distinct()
            .ToList();

        // Add resolved roles to subjects
        subjects.AddRange(resolvedRoles);

        // Get all permission policies for this workstream and resolved subjects
        var policies = await _context.CasbinPolicies
            .Where(p => (p.WorkstreamId == workstreamId || p.WorkstreamId == "*") &&
                       p.PolicyType == "p" &&
                       subjects.Contains(p.V0!))
            .ToListAsync();

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

    private ScenarioDefinition? GetScenarioDefinition(string scenarioName, string workstreamId)
    {
        // This method is kept for backward compatibility but logs a warning
        _logger.LogWarning("GetScenarioDefinition called without token - this should not happen in the new design");
        return null;
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

public class DynamicScenario
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public List<string> AvailableActions { get; set; } = [];
    public string WorkstreamId { get; set; } = string.Empty;
}
