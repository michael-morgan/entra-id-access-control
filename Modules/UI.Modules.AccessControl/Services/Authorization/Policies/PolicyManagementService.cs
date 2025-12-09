using UI.Modules.AccessControl.Services.Graph;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using Microsoft.Extensions.Logging;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Authorization.Policies;

/// <summary>
/// Service for managing Casbin policies with business logic.
/// Orchestrates repository calls, Graph API lookups, and ViewModel mapping.
/// </summary>
public class PolicyManagementService(
    IPolicyRepository policyRepository,
    IRoleRepository roleRepository,
    IResourceRepository resourceRepository,
    CachedGraphUserService userService,
    CachedGraphGroupService groupService,
    ILogger<PolicyManagementService> logger) : IPolicyManagementService
{
    private readonly IPolicyRepository _policyRepository = policyRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly IResourceRepository _resourceRepository = resourceRepository;
    private readonly CachedGraphUserService _userService = userService;
    private readonly CachedGraphGroupService _groupService = groupService;
    private readonly ILogger<PolicyManagementService> _logger = logger;

    /// <inheritdoc />
    public async Task<(IEnumerable<CasbinPolicy> Policies, Dictionary<string, string> SubjectDisplayNames, IEnumerable<string> PolicyTypes)>
        GetPoliciesWithDisplayNamesAsync(string workstream, string? policyType = null, string? search = null)
    {
        // Get policies from repository
        var policies = await _policyRepository.SearchAsync(workstream, policyType, search);
        var policiesList = policies.ToList();

        // Get available policy types
        var policyTypes = await _policyRepository.GetPolicyTypesAsync(workstream);

        // Resolve subject display names from Graph API
        var displayNames = await ResolveSubjectDisplayNamesAsync(policiesList);

        return (policiesList, displayNames, policyTypes);
    }

    /// <inheritdoc />
    public async Task<(CasbinPolicy? Policy, Dictionary<string, string> SubjectDisplayNames)?>
        GetPolicyByIdWithDisplayNamesAsync(int id)
    {
        var policy = await _policyRepository.GetByIdAsync(id);
        if (policy == null)
        {
            return null;
        }

        var displayNames = await ResolveSubjectDisplayNamesAsync([policy]);
        return (policy, displayNames);
    }

    /// <inheritdoc />
    public async Task<CasbinPolicy> CreatePolicyAsync(PolicyViewModel model, string workstream, string createdBy)
    {
        var policy = new CasbinPolicy
        {
            PolicyType = model.PolicyType,
            V0 = model.V0,
            V1 = model.V1,
            V2 = model.V2,
            V3 = model.V3,
            V4 = model.V4,
            V5 = model.V5,
            WorkstreamId = workstream,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            ModifiedBy = createdBy
        };

        return await _policyRepository.CreateAsync(policy);
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePolicyAsync(int id, PolicyViewModel model, string modifiedBy)
    {
        var policy = await _policyRepository.GetByIdAsync(id);
        if (policy == null)
        {
            return false;
        }

        policy.PolicyType = model.PolicyType;
        policy.V0 = model.V0;
        policy.V1 = model.V1;
        policy.V2 = model.V2;
        policy.V3 = model.V3;
        policy.V4 = model.V4;
        policy.V5 = model.V5;
        policy.ModifiedBy = modifiedBy;
        policy.ModifiedAt = DateTimeOffset.UtcNow;

        await _policyRepository.UpdateAsync(policy);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePolicyAsync(int id)
    {
        var policy = await _policyRepository.GetByIdAsync(id);
        if (policy == null)
        {
            return false;
        }

        await _policyRepository.DeleteAsync(id);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> PolicyExistsAsync(int id)
    {
        return await _policyRepository.ExistsAsync(id);
    }

    /// <summary>
    /// Resolves display names for policy subjects (V0, V1) from Graph API.
    /// Tries to match GUIDs to groups first, then users.
    /// </summary>
    private async Task<Dictionary<string, string>> ResolveSubjectDisplayNamesAsync(IEnumerable<CasbinPolicy> policies)
    {
        var displayNames = new Dictionary<string, string>();

        // Extract potential group/user IDs from V0 and V1
        var potentialIds = policies
            .SelectMany(p => new[] { p.V0, p.V1 })
            .Where(v => !string.IsNullOrEmpty(v) && Guid.TryParse(v!, out _))
            .Select(v => v!)
            .Distinct()
            .ToList();

        if (potentialIds.Count == 0)
        {
            return displayNames;
        }

        try
        {
            // Try to fetch as groups first
            var groups = await _groupService.GetGroupsByIdsAsync(potentialIds);
            foreach (var kvp in groups)
            {
                displayNames[kvp.Key] = kvp.Value.DisplayName ?? kvp.Value.MailNickname ?? kvp.Key;
            }

            // For IDs not found as groups, try users
            var notFoundIds = potentialIds.Except(displayNames.Keys).ToList();
            if (notFoundIds.Count != 0)
            {
                var users = await _userService.GetUsersByIdsAsync(notFoundIds);
                foreach (var kvp in users)
                {
                    displayNames[kvp.Key] = kvp.Value.DisplayName ?? kvp.Value.UserPrincipalName ?? kvp.Key;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch display names from Graph API for policy subjects");
        }

        return displayNames;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableRoleNamesAsync(string workstream)
    {
        var roles = await _roleRepository.SearchAsync(workstream, search: null);
        return roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoleName)
            .Select(r => r.RoleName)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableResourcePatternsAsync(string workstream)
    {
        var resources = await _resourceRepository.SearchAsync(
            workstream: workstream,
            workstreamFilter: null,
            search: null);

        return resources
            .Where(r => r.WorkstreamId == workstream || r.WorkstreamId == "*")
            .OrderBy(r => r.ResourcePattern)
            .Select(r => r.ResourcePattern)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAvailableWorkstreamsAsync()
    {
        return await _policyRepository.GetWorkstreamsAsync();
    }
}
