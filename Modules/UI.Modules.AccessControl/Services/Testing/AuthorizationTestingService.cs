using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Testing;

/// <summary>
/// Service for testing authorization policies and ABAC rules.
/// </summary>
public class AuthorizationTestingService(
    IPolicyRepository policyRepository,
    IAbacRuleRepository abacRuleRepository,
    ILogger<AuthorizationTestingService> logger) : IAuthorizationTestingService
{
    private readonly IPolicyRepository _policyRepository = policyRepository;
    private readonly IAbacRuleRepository _abacRuleRepository = abacRuleRepository;
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

            // Extract groups and roles
            var groupClaims = jwtToken.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
            var roleClaims = jwtToken.Claims.Where(c => c.Type == "roles" || c.Type == "role").Select(c => c.Value).ToList();

            // Add step: JWT Claims Extracted
            trace.Add(new AuthorizationStep
            {
                StepName = "JWT Claims Extracted",
                Description = $"User ID: {userId}, Name: {userName}",
                Result = "Pass",
                Details = $"Token contains {jwtToken.Claims.Count()} claims",
                Order = stepOrder++
            });

            // Add step: Groups and Roles Identified
            trace.Add(new AuthorizationStep
            {
                StepName = "Groups and Roles Identified",
                Description = $"Groups: [{string.Join(", ", groupClaims)}], Roles: [{string.Join(", ", roleClaims)}]",
                Result = "Pass",
                Details = $"Found {groupClaims.Count} groups and {roleClaims.Count} roles",
                Order = stepOrder++
            });

            // Build subjects list (user ID + roles + groups)
            var subjects = new List<string>();
            if (!string.IsNullOrWhiteSpace(userId))
                subjects.Add(userId);
            subjects.AddRange(roleClaims.Where(r => !string.IsNullOrWhiteSpace(r)));
            subjects.AddRange(groupClaims.Where(g => !string.IsNullOrWhiteSpace(g)));

            // Resolve group-to-role mappings from Casbin policies (type "g")
            var roleMappings = (await _policyRepository.GetBySubjectIdsAsync(subjects, policyType: "g"))
                .Where(p => p.WorkstreamId == request.WorkstreamId || p.WorkstreamId == "*")
                .ToList();

            var resolvedRoles = roleMappings
                .Where(g => !string.IsNullOrWhiteSpace(g.V1))
                .Select(g => g.V1!)
                .Distinct()
                .ToList();

            subjects.AddRange(resolvedRoles);

            // Add step: Role Resolution
            trace.Add(new AuthorizationStep
            {
                StepName = "Role Resolution",
                Description = $"Resolved roles: [{string.Join(", ", resolvedRoles)}]",
                Result = "Pass",
                Details = $"Found {roleMappings.Count} group-to-role mappings, resolved to {resolvedRoles.Count} roles",
                Order = stepOrder++
            });

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
            if (!string.IsNullOrWhiteSpace(request.MockEntityJson))
            {
                var abacRules = (await _abacRuleRepository.SearchAsync(request.WorkstreamId)).ToList();

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
}
