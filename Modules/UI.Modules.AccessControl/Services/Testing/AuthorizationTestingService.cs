using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using Api.Modules.AccessControl.Authorization;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Api.Modules.AccessControl.Interfaces;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Testing;

/// <summary>
/// Service for testing authorization policies and ABAC rules.
/// </summary>
public class AuthorizationTestingService(
    IPolicyRepository policyRepository,
    IAbacRuleRepository abacRuleRepository,
    IUserAttributeRepository userAttributeRepository,
    IGroupAttributeRepository groupAttributeRepository,
    IRoleAttributeRepository roleAttributeRepository,
    AccessControlDbContext accessControlDbContext,
    ILoggerFactory loggerFactory,
    IEnumerable<IWorkstreamAbacEvaluator> workstreamEvaluators,
    ILogger<AuthorizationTestingService> logger) : IAuthorizationTestingService
{
    private readonly IPolicyRepository _policyRepository = policyRepository;
    private readonly IAbacRuleRepository _abacRuleRepository = abacRuleRepository;
    private readonly IUserAttributeRepository _userAttributeRepository = userAttributeRepository;
    private readonly IGroupAttributeRepository _groupAttributeRepository = groupAttributeRepository;
    private readonly IRoleAttributeRepository _roleAttributeRepository = roleAttributeRepository;
    private readonly AccessControlDbContext _accessControlDbContext = accessControlDbContext;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IEnumerable<IWorkstreamAbacEvaluator> _workstreamEvaluators = workstreamEvaluators;
    private readonly ILogger<AuthorizationTestingService> _logger = logger;

    public async Task<AuthorizationTestResult> CheckAuthorizationAsync(AuthorizationTestRequest request)
    {
        var result = new AuthorizationTestResult
        {
            Success = true,
            IsAuthorized = false,
            TestDescription = request.TestDescription ?? $"Test {request.Action} on {request.Resource}",
            Resource = request.Resource,
            Action = request.Action,
            WorkstreamId = request.WorkstreamId,
            MockEntityJson = request.MockEntityJson,
            EvaluationTrace = new List<AuthorizationStep>()
        };

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                result.Success = false;
                result.ErrorMessage = "Token is required";
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.WorkstreamId))
            {
                result.Success = false;
                result.ErrorMessage = "Workstream is required";
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.Resource))
            {
                result.Success = false;
                result.ErrorMessage = "Resource is required";
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.Action))
            {
                result.Success = false;
                result.ErrorMessage = "Action is required";
                return result;
            }

            var trace = new List<AuthorizationStep>();
            var stepOrder = 0;

            // Decode JWT token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(request.Token);

            // Extract user identity
            var oidClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid" ||
                c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
            var userId = oidClaim?.Value ?? string.Empty;

            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name");
            var userName = nameClaim?.Value ?? "Unknown User";

            if (string.IsNullOrWhiteSpace(userId))
            {
                result.Success = false;
                result.ErrorMessage = "User ID not found in token";
                return result;
            }

            // Extract groups from JWT (roles come from Casbin, not JWT)
            var groupClaims = jwtToken.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();

            // Add step: JWT Claims Extracted
            trace.Add(new AuthorizationStep
            {
                StepName = "JWT Claims Extracted",
                Description = $"User ID: {userId}, Name: {userName}",
                Result = "Pass",
                Details = $"Token contains {jwtToken.Claims.Count()} claims, {groupClaims.Count} groups",
                Order = stepOrder++
            });

            // Resolve roles from Casbin policies based on group memberships
            var resolvedRoles = await ResolveRolesFromGroupsAsync(groupClaims, request.WorkstreamId);

            // Add step: Role Resolution
            trace.Add(new AuthorizationStep
            {
                StepName = "Role Resolution (Casbin)",
                Description = $"Resolved {resolvedRoles.Count} roles from groups: [{string.Join(", ", resolvedRoles)}]",
                Result = "Pass",
                Details = $"Used Casbin policy engine to resolve roles from {groupClaims.Count} groups",
                Order = stepOrder++
            });

            // Build subjects list (user ID + resolved roles + groups)
            var subjects = new List<string>();
            if (!string.IsNullOrWhiteSpace(userId))
                subjects.Add(userId);
            subjects.AddRange(resolvedRoles.Where(r => !string.IsNullOrWhiteSpace(r)));
            subjects.AddRange(groupClaims.Where(g => !string.IsNullOrWhiteSpace(g)));

            // Check RBAC policies (Casbin type "p")
            // V0=Subject, V1=Workstream, V2=Resource, V3=Action, V4=Effect
            var policies = (await _policyRepository.GetBySubjectIdsAsync(subjects, policyType: "p"))
                .Where(p => p.WorkstreamId == request.WorkstreamId || p.WorkstreamId == "*")
                .ToList();

            var matchingPermissions = policies
                .Where(p => p.PolicyType == "p" &&
                           subjects.Contains(p.V0 ?? "") &&
                           (p.V2 == request.Resource || p.V2 == "*") &&
                           (p.V3 == request.Action || p.V3 == "*"))
                .ToList();

            var rbacAllowed = matchingPermissions.Count != 0;

            // Add step: RBAC Check
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

            // Evaluate ABAC rules (if entity provided)
            var abacAllowed = true;
            string? abacDenyReason = null;

            if (!string.IsNullOrWhiteSpace(request.MockEntityJson))
            {
                try
                {
                    // Build ABAC context
                    var abacContext = await BuildAbacContextAsync(
                        userId,
                        userName,
                        groupClaims,
                        resolvedRoles,
                        request.WorkstreamId,
                        request.MockEntityJson);

                    // Try custom workstream evaluator first
                    var customEvaluator = _workstreamEvaluators.FirstOrDefault(e => e.WorkstreamId == request.WorkstreamId);
                    AbacEvaluationResult? customResult = null;

                    if (customEvaluator != null)
                    {
                        customResult = await customEvaluator.EvaluateAsync(
                            abacContext,
                            request.Resource,
                            request.Action,
                            CancellationToken.None);

                        if (customResult != null)
                        {
                            trace.Add(new AuthorizationStep
                            {
                                StepName = "Custom ABAC Evaluator",
                                Description = customResult.Allowed
                                    ? $"Custom evaluator PASSED: {customResult.Reason}"
                                    : $"Custom evaluator DENIED: {customResult.Reason}",
                                Result = customResult.Allowed ? "Pass" : "Fail",
                                Details = $"Workstream '{request.WorkstreamId}' custom evaluator: {customResult.Message ?? customResult.Reason}",
                                Order = stepOrder++
                            });

                            abacAllowed = customResult.Allowed;
                            abacDenyReason = customResult.Reason;
                        }
                        else
                        {
                            trace.Add(new AuthorizationStep
                            {
                                StepName = "Custom ABAC Evaluator",
                                Description = "Custom evaluator skipped this resource/action",
                                Result = "Skip",
                                Details = $"Workstream '{request.WorkstreamId}' custom evaluator returned null - not handling this case",
                                Order = stepOrder++
                            });
                        }
                    }

                    // Evaluate generic ABAC rules (always run)
                    var genericEvaluator = new GenericAbacEvaluator(
                        request.WorkstreamId,
                        _accessControlDbContext,
                        _loggerFactory.CreateLogger<GenericAbacEvaluator>());

                    var genericResult = await genericEvaluator.EvaluateAsync(
                        abacContext,
                        request.Resource,
                        request.Action,
                        CancellationToken.None);

                    if (genericResult != null)
                    {
                        trace.Add(new AuthorizationStep
                        {
                            StepName = "Generic ABAC Evaluator",
                            Description = genericResult.Allowed
                                ? $"Generic ABAC rules PASSED: {genericResult.Reason}"
                                : $"Generic ABAC rules DENIED: {genericResult.Reason}",
                            Result = genericResult.Allowed ? "Pass" : "Fail",
                            Details = genericResult.Message ?? genericResult.Reason ?? "Database-driven ABAC rules evaluated",
                            Order = stepOrder++
                        });

                        // Both custom and generic must pass (AND logic)
                        if (customResult != null)
                        {
                            abacAllowed = abacAllowed && genericResult.Allowed;
                            if (!genericResult.Allowed)
                            {
                                abacDenyReason = genericResult.Reason;
                            }
                        }
                        else
                        {
                            // No custom result, use generic result
                            abacAllowed = genericResult.Allowed;
                            abacDenyReason = genericResult.Reason;
                        }

                        // Store generic result for diagnostic extraction later
                        result.DiagnosticMessage = FormatDiagnostics(genericResult.Diagnostics, rbacAllowed, abacAllowed);
                    }
                    else
                    {
                        trace.Add(new AuthorizationStep
                        {
                            StepName = "Generic ABAC Evaluator",
                            Description = "No applicable generic ABAC rules found",
                            Result = "Skip",
                            Details = "Generic evaluator returned null - no database rules apply",
                            Order = stepOrder++
                        });

                        // If no generic rules and no custom rules, ABAC passes by default
                        if (customResult == null)
                        {
                            trace.Add(new AuthorizationStep
                            {
                                StepName = "ABAC Summary",
                                Description = "No ABAC rules evaluated - defaulting to RBAC decision only",
                                Result = "Skip",
                                Details = "Neither custom nor generic ABAC evaluators returned a result",
                                Order = stepOrder++
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during ABAC evaluation");
                    trace.Add(new AuthorizationStep
                    {
                        StepName = "ABAC Evaluation Error",
                        Description = "ABAC evaluation failed with error",
                        Result = "Fail",
                        Details = $"Error: {ex.Message}",
                        Order = stepOrder++
                    });
                    abacAllowed = false;
                    abacDenyReason = $"ABAC evaluation error: {ex.Message}";
                }
            }
            else
            {
                trace.Add(new AuthorizationStep
                {
                    StepName = "ABAC Evaluation",
                    Description = "No entity provided - ABAC rules skipped",
                    Result = "Skip",
                    Details = "MockEntityJson not provided in request",
                    Order = stepOrder++
                });
            }

            // Final authorization decision
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

            _logger.LogInformation(
                "Authorization check completed for user {UserId} on {Resource}/{Action} in workstream {Workstream}: {Result}",
                userId, request.Resource, request.Action, request.WorkstreamId, result.IsAuthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authorization");
            result.Success = false;
            result.ErrorMessage = $"Error checking authorization: {ex.Message}";
            result.EvaluationTrace.Add(new AuthorizationStep
            {
                StepName = "Error",
                Description = "Exception occurred during authorization check",
                Result = "Fail",
                Details = ex.Message,
                Order = 999
            });
        }

        return result;
    }

    /// <summary>
    /// Builds an ABAC context for testing by merging user attributes with mock entity data.
    /// </summary>
    private async Task<AbacContext> BuildAbacContextAsync(
        string userId,
        string? userName,
        List<string> groupIds,
        List<string> roles,
        string workstreamId,
        string mockEntityJson)
    {
        // Parse mock entity JSON into resource attributes
        var resourceAttributes = new Dictionary<string, object>();
        try
        {
            _logger.LogInformation("[ABAC CONTEXT] Parsing mock entity JSON: {Json}", mockEntityJson);
            var mockEntity = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(mockEntityJson);
            if (mockEntity != null)
            {
                foreach (var kvp in mockEntity)
                {
                    resourceAttributes[kvp.Key] = kvp.Value.ValueKind switch
                    {
                        JsonValueKind.Number => kvp.Value.GetDecimal(),
                        JsonValueKind.String => kvp.Value.GetString() ?? "",
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => kvp.Value.ToString()
                    };
                    _logger.LogInformation("[ABAC CONTEXT] Added resource attribute: {Key} = {Value}", kvp.Key, resourceAttributes[kvp.Key]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse mock entity JSON");
        }

        _logger.LogInformation("[ABAC CONTEXT] Total resource attributes: {Count}", resourceAttributes.Count);

        // Load user attributes from database
        var userAttributes = new Dictionary<string, object>();

        // 1. Get user-specific attributes
        var userAttr = await _userAttributeRepository.GetByUserIdAndWorkstreamAsync(userId, workstreamId);
        if (userAttr != null && !string.IsNullOrEmpty(userAttr.AttributesJson))
        {
            try
            {
                var attrs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(userAttr.AttributesJson);
                if (attrs != null)
                {
                    foreach (var kvp in attrs)
                    {
                        userAttributes[kvp.Key] = kvp.Value.ValueKind switch
                        {
                            JsonValueKind.Number => kvp.Value.GetDecimal(),
                            JsonValueKind.String => kvp.Value.GetString() ?? "",
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => kvp.Value.ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse user attributes JSON");
            }
        }

        // 2. Get group attributes (lower precedence than user)
        foreach (var groupId in groupIds)
        {
            var groupAttr = await _groupAttributeRepository.GetByGroupIdAndWorkstreamAsync(groupId, workstreamId);
            if (groupAttr != null && !string.IsNullOrEmpty(groupAttr.AttributesJson))
            {
                try
                {
                    var attrs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(groupAttr.AttributesJson);
                    if (attrs != null)
                    {
                        foreach (var kvp in attrs)
                        {
                            // Only add if not already present (user attributes take precedence)
                            if (!userAttributes.ContainsKey(kvp.Key))
                            {
                                userAttributes[kvp.Key] = kvp.Value.ValueKind switch
                                {
                                    JsonValueKind.Number => kvp.Value.GetDecimal(),
                                    JsonValueKind.String => kvp.Value.GetString() ?? "",
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    _ => kvp.Value.ToString()
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse group attributes JSON for group {GroupId}", groupId);
                }
            }
        }

        // 3. Get role attributes (lowest precedence)
        var allRoleAttrs = await _roleAttributeRepository.SearchAsync(workstreamId);
        foreach (var role in roles)
        {
            var roleAttr = allRoleAttrs.FirstOrDefault(r => r.RoleId == role);
            if (roleAttr != null && !string.IsNullOrEmpty(roleAttr.AttributesJson))
            {
                try
                {
                    var attrs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(roleAttr.AttributesJson);
                    if (attrs != null)
                    {
                        foreach (var kvp in attrs)
                        {
                            // Only add if not already present (user and group attributes take precedence)
                            if (!userAttributes.ContainsKey(kvp.Key))
                            {
                                userAttributes[kvp.Key] = kvp.Value.ValueKind switch
                                {
                                    JsonValueKind.Number => kvp.Value.GetDecimal(),
                                    JsonValueKind.String => kvp.Value.GetString() ?? "",
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    _ => kvp.Value.ToString()
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse role attributes JSON for role {RoleId}", role);
                }
            }
        }

        // Build and return ABAC context
        _logger.LogInformation("[ABAC CONTEXT] Final user attributes count: {Count}", userAttributes.Count);
        _logger.LogInformation("[ABAC CONTEXT] User attributes: {Attrs}", string.Join(", ", userAttributes.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        _logger.LogInformation("[ABAC CONTEXT] Resource attributes: {Attrs}", string.Join(", ", resourceAttributes.Select(kvp => $"{kvp.Key}={kvp.Value}")));

        return new AbacContext
        {
            UserId = userId,
            UserDisplayName = userName,
            UserEmail = null,
            Roles = [.. roles],
            Groups = [.. groupIds],
            UserAttributes = userAttributes,
            ResourceAttributes = resourceAttributes,
            RequestTime = DateTimeOffset.UtcNow,
            ClientIpAddress = "127.0.0.1",
            IsBusinessHours = true, // Assume business hours for testing
            IsInternalNetwork = true // Assume internal network for testing
        };
    }

    private static string FormatPolicyDisplay(Api.Modules.AccessControl.Persistence.Entities.Authorization.CasbinPolicy policy)
    {
        return policy.PolicyType switch
        {
            "p" => $"{policy.V0}, {policy.V1}, {policy.V2}",
            "g" => $"{policy.V0} → {policy.V1}" + (policy.V2 != null ? $" ({policy.V2})" : ""),
            "g2" => $"{policy.V0} → {policy.V1} in domain {policy.V2}",
            _ => $"{policy.V0}, {policy.V1}, {policy.V2}"
        };
    }

    /// <summary>
    /// Resolves all roles for the user's groups from Casbin policies.
    /// Includes both direct and inherited roles.
    /// </summary>
    private async Task<List<string>> ResolveRolesFromGroupsAsync(List<string> groupIds, string workstreamId)
    {
        if (groupIds.Count == 0)
            return [];

        var allRoles = new List<string>();
        var visited = new HashSet<string>();

        // Resolve roles for each group recursively
        foreach (var groupId in groupIds)
        {
            await ResolveRolesRecursivelyAsync(groupId, workstreamId, allRoles, visited);
        }

        // Return distinct roles
        return allRoles.Distinct().ToList();
    }

    /// <summary>
    /// Recursively resolves all roles for a subject (group or role), including inherited roles.
    /// </summary>
    private async Task ResolveRolesRecursivelyAsync(
        string subject,
        string workstreamId,
        List<string> allRoles,
        HashSet<string> visited)
    {
        // Prevent infinite loops in case of circular role dependencies
        if (!visited.Add(subject))
            return;

        // Get direct roles from policies (fetch 'g' policies where V0=subject and V2=workstream)
        var policies = await _policyRepository.GetRolesForSubjectsAsync(new[] { subject }, workstreamId);

        foreach (var policy in policies)
        {
            var role = policy.V1; // V1 contains the role name in 'g' policies
            if (string.IsNullOrWhiteSpace(role))
                continue;

            // Add the role if not already present
            if (!allRoles.Contains(role))
            {
                allRoles.Add(role);

                // Recursively resolve inherited roles (role-to-role inheritance)
                await ResolveRolesRecursivelyAsync(role, workstreamId, allRoles, visited);
            }
        }
    }

    /// <summary>
    /// Formats ABAC diagnostics into a user-friendly message for display in UI.
    /// </summary>
    private static string? FormatDiagnostics(AbacEvaluationDiagnostics? diagnostics, bool rbacAllowed, bool abacAllowed)
    {
        if (diagnostics == null)
            return null;

        var lines = new List<string>();

        // Add matched rule groups
        if (diagnostics.MatchedRuleGroups.Any())
        {
            lines.Add("✓ Matched Rule Groups:");
            foreach (var group in diagnostics.MatchedRuleGroups)
            {
                lines.Add($"  - {group.GroupName} ({group.Resource} + {group.Action}) - {group.RuleCount} rule(s)");
            }
        }

        // Add evaluated rules
        if (diagnostics.EvaluatedRules.Any())
        {
            lines.Add("");
            lines.Add("✓ Evaluated Rules:");
            foreach (var rule in diagnostics.EvaluatedRules)
            {
                var icon = rule.Passed ? "✓" : "✗";
                lines.Add($"  {icon} {rule.RuleName} ({rule.RuleType}): {(rule.Passed ? "Passed" : "Failed")}");
                if (!string.IsNullOrEmpty(rule.Reason))
                {
                    lines.Add($"      {rule.Reason}");
                }
            }
        }

        // Add missing validations
        if (diagnostics.MissingValidations.Any())
        {
            lines.Add("");
            lines.Add("⚠ Missing Validations Detected:");

            var criticalSeverity = diagnostics.MissingValidations.Where(m => m.Severity == "Critical").ToList();
            var highSeverity = diagnostics.MissingValidations.Where(m => m.Severity == "High").ToList();
            var mediumSeverity = diagnostics.MissingValidations.Where(m => m.Severity == "Medium").ToList();
            var lowSeverity = diagnostics.MissingValidations.Where(m => m.Severity == "Low").ToList();

            if (criticalSeverity.Any())
            {
                lines.Add("  CRITICAL (No ABAC Rule Group):");
                foreach (var missing in criticalSeverity)
                {
                    lines.Add($"    🚨 {missing.AttributeName} ({missing.AttributeSource})");
                    lines.Add($"        Reason: {missing.Reason}");
                    if (!string.IsNullOrEmpty(missing.RecommendedRuleType))
                    {
                        lines.Add($"        Recommendation: Add {missing.RecommendedRuleType} rule");
                    }
                }
            }

            if (highSeverity.Any())
            {
                lines.Add("  High Priority (Required Attributes):");
                foreach (var missing in highSeverity)
                {
                    lines.Add($"    ✗ {missing.AttributeName} ({missing.AttributeSource})");
                    lines.Add($"        Reason: {missing.Reason}");
                    if (!string.IsNullOrEmpty(missing.RecommendedRuleType))
                    {
                        lines.Add($"        Recommendation: Add {missing.RecommendedRuleType} rule");
                    }
                    if (missing.ExistsInActions.Any())
                    {
                        lines.Add($"        ℹ Already validated in: {string.Join(", ", missing.ExistsInActions)}");
                    }
                }
            }

            if (mediumSeverity.Any())
            {
                lines.Add("  Medium Priority (Common Patterns):");
                foreach (var missing in mediumSeverity)
                {
                    lines.Add($"    ⚠ {missing.AttributeName} ({missing.AttributeSource})");
                    lines.Add($"        Reason: {missing.Reason}");
                    if (missing.ExistsInActions.Any())
                    {
                        lines.Add($"        ℹ Validated in: {string.Join(", ", missing.ExistsInActions)}");
                    }
                }
            }

            if (lowSeverity.Any())
            {
                lines.Add("  Low Priority (Suggestions):");
                foreach (var missing in lowSeverity)
                {
                    lines.Add($"    ℹ {missing.AttributeName}: {missing.Reason}");
                }
            }
        }

        // Add suggestions
        if (diagnostics.Suggestions.Any())
        {
            lines.Add("");
            lines.Add("Suggestions:");
            foreach (var suggestion in diagnostics.Suggestions)
            {
                lines.Add($"  → {suggestion}");
            }
        }

        return lines.Any() ? string.Join("\n", lines) : null;
    }
}
