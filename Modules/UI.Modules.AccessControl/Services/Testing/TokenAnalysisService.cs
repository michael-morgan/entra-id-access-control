using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Services.Graph;

namespace UI.Modules.AccessControl.Services.Testing;

/// <summary>
/// Service for analyzing JWT tokens and extracting access control context.
/// </summary>
public class TokenAnalysisService(
    IUserAttributeRepository userAttributeRepository,
    IGroupAttributeRepository groupAttributeRepository,
    IRoleAttributeRepository roleAttributeRepository,
    IPolicyRepository policyRepository,
    IAbacRuleRepository abacRuleRepository,
    IAbacRuleGroupRepository abacRuleGroupRepository,
    CachedGraphUserService cachedGraphUserService,
    CachedGraphGroupService cachedGraphGroupService,
    ILogger<TokenAnalysisService> logger) : ITokenAnalysisService
{
    private readonly IUserAttributeRepository _userAttributeRepository = userAttributeRepository;
    private readonly IGroupAttributeRepository _groupAttributeRepository = groupAttributeRepository;
    private readonly IRoleAttributeRepository _roleAttributeRepository = roleAttributeRepository;
    private readonly IPolicyRepository _policyRepository = policyRepository;
    private readonly IAbacRuleRepository _abacRuleRepository = abacRuleRepository;
    private readonly IAbacRuleGroupRepository _abacRuleGroupRepository = abacRuleGroupRepository;
    private readonly CachedGraphUserService _cachedGraphUserService = cachedGraphUserService;
    private readonly CachedGraphGroupService _cachedGraphGroupService = cachedGraphGroupService;
    private readonly ILogger<TokenAnalysisService> _logger = logger;

    /// <inheritdoc/>
    public async Task<TokenAnalysisResult> AnalyzeTokenAsync(string token, string workstreamId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new TokenAnalysisResult
            {
                Success = false,
                ErrorMessage = "Token is required."
            };
        }

        if (string.IsNullOrWhiteSpace(workstreamId))
        {
            return new TokenAnalysisResult
            {
                Success = false,
                ErrorMessage = "Workstream ID is required."
            };
        }

        try
        {
            // Decode JWT token without validation (display-only)
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

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

            // Extract groups and roles
            result.Groups = [.. jwtToken.Claims.Where(c => c.Type == "groups").Select(c => c.Value)];
            result.Roles = [.. jwtToken.Claims.Where(c => c.Type == "roles" || c.Type == "role").Select(c => c.Value)];

            // Resolve group friendly names from Graph API
            if (result.Groups.Count > 0)
            {
                var groupsDict = await _cachedGraphGroupService.GetGroupsByIdsAsync(result.Groups);
                result.GroupsWithNames = result.Groups.Select(groupId =>
                {
                    var displayName = groupsDict.TryGetValue(groupId, out var group) && group.DisplayName != null
                        ? group.DisplayName
                        : groupId; // Fallback to ID if not found
                    return new GroupReference { Id = groupId, DisplayName = displayName };
                }).ToList();
            }

            // Load attributes
            await LoadUserAttributesAsync(result, workstreamId);
            await LoadRoleAttributesAsync(result, workstreamId);
            await LoadGroupAttributesAsync(result, workstreamId);

            // Merge attributes with precedence: User > Role > Group
            result.MergedAttributes = MergeAttributes(result);

            // Load policies and ABAC rules
            await LoadApplicablePoliciesAsync(result, workstreamId);
            await LoadAbacRulesAsync(result, workstreamId);
            await LoadAbacRuleGroupsAsync(result, workstreamId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing token");
            return new TokenAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Error analyzing token: {ex.Message}"
            };
        }
    }

    private async Task LoadUserAttributesAsync(TokenAnalysisResult result, string workstreamId)
    {
        if (string.IsNullOrWhiteSpace(result.UserId))
            return;

        var userAttrs = await _userAttributeRepository.GetByUserIdAndWorkstreamAsync(result.UserId, workstreamId);

        if (userAttrs != null && !string.IsNullOrWhiteSpace(userAttrs.AttributesJson))
        {
            // Resolve user display name from Graph API
            var user = await _cachedGraphUserService.GetUserByIdAsync(userAttrs.UserId);
            var userDisplayName = user?.DisplayName ?? result.UserName ?? userAttrs.UserId;

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
                            EntityId = userAttrs.UserId,
                            EntityName = userDisplayName
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

    private async Task LoadRoleAttributesAsync(TokenAnalysisResult result, string workstreamId)
    {
        if (result.Roles.Count == 0)
            return;

        // Get all role attributes for the workstream and filter by role values
        var allRoleAttrs = await _roleAttributeRepository.SearchAsync(workstreamId);
        var roleAttrs = allRoleAttrs.Where(ra => result.Roles.Contains(ra.RoleValue)).ToList();

        foreach (var roleAttr in roleAttrs)
        {
            if (string.IsNullOrWhiteSpace(roleAttr.AttributesJson))
                continue;

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
                            EntityId = roleAttr.RoleValue,
                            EntityName = roleAttr.RoleValue // Role values are already human-readable
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

    private async Task LoadGroupAttributesAsync(TokenAnalysisResult result, string workstreamId)
    {
        if (result.Groups.Count == 0)
            return;

        // Get all group attributes for the workstream and filter by group IDs
        var allGroupAttrs = await _groupAttributeRepository.SearchAsync(workstreamId);
        var groupAttrs = allGroupAttrs.Where(ga => result.Groups.Contains(ga.GroupId)).ToList();

        // Batch fetch group display names from Graph API
        var groupIds = groupAttrs.Select(ga => ga.GroupId).Distinct().ToList();
        var groupsDict = await _cachedGraphGroupService.GetGroupsByIdsAsync(groupIds);

        foreach (var groupAttr in groupAttrs)
        {
            if (string.IsNullOrWhiteSpace(groupAttr.AttributesJson))
                continue;

            // Resolve group display name (prefer Graph API, fallback to database GroupName)
            var groupDisplayName = groupsDict.TryGetValue(groupAttr.GroupId, out var group) && group.DisplayName != null
                ? group.DisplayName
                : groupAttr.GroupName ?? groupAttr.GroupId;

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
                            EntityId = groupAttr.GroupId,
                            EntityName = groupDisplayName
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

    private static Dictionary<string, AttributeValue> MergeAttributes(TokenAnalysisResult result)
    {
        var mergedAttributes = new Dictionary<string, AttributeValue>();

        // Start with group attributes (lowest precedence)
        foreach (var attr in result.GroupAttributes)
        {
            mergedAttributes[attr.AttributeName] = new AttributeValue
            {
                Value = attr.Value,
                Source = attr.Source,
                EntityId = attr.EntityId,
                EntityName = attr.EntityName
            };
        }

        // Override with role attributes
        foreach (var attr in result.RoleAttributes)
        {
            mergedAttributes[attr.AttributeName] = new AttributeValue
            {
                Value = attr.Value,
                Source = attr.Source,
                EntityId = attr.EntityId,
                EntityName = attr.EntityName
            };
        }

        // Override with user attributes (highest precedence)
        foreach (var attr in result.UserAttributes)
        {
            mergedAttributes[attr.AttributeName] = new AttributeValue
            {
                Value = attr.Value,
                Source = attr.Source,
                EntityId = attr.EntityId,
                EntityName = attr.EntityName
            };
        }

        return mergedAttributes;
    }

    private async Task LoadApplicablePoliciesAsync(TokenAnalysisResult result, string workstreamId)
    {
        var subjects = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.UserId))
            subjects.Add(result.UserId);
        subjects.AddRange(result.Roles.Where(r => !string.IsNullOrWhiteSpace(r)));
        subjects.AddRange(result.Groups.Where(g => !string.IsNullOrWhiteSpace(g)));

        // Get policies by subject IDs - this queries V0 field
        var policiesBySubject = await _policyRepository.GetBySubjectIdsAsync(subjects);

        // Also get all policies for the workstream to check V1 matches
        var allPolicies = await _policyRepository.SearchAsync(workstreamId);
        var policiesWithV1Match = allPolicies.Where(p => p.V1 != null && subjects.Contains(p.V1));

        // Combine and deduplicate
        var policies = policiesBySubject
            .Concat(policiesWithV1Match)
            .Distinct()
            .OrderBy(p => p.PolicyType)
            .ThenBy(p => p.V0)
            .ToList();

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
    }

    private async Task LoadAbacRulesAsync(TokenAnalysisResult result, string workstreamId)
    {
        var abacRules = await _abacRuleRepository.SearchAsync(workstreamId);

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
    }

    private async Task LoadAbacRuleGroupsAsync(TokenAnalysisResult result, string workstreamId)
    {
        var ruleGroups = await _abacRuleGroupRepository.SearchAsync(workstreamId);

        result.ApplicableRuleGroups = [.. ruleGroups.Select(rg => new AbacRuleGroupSummary
        {
            Id = rg.Id,
            GroupName = rg.GroupName,
            LogicOperator = rg.LogicalOperator,
            RuleCount = rg.Rules?.Count ?? 0,
            WorkstreamId = rg.WorkstreamId
        })];
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
        return $"{rule.RuleName} ({rule.RuleType})";
    }
}
