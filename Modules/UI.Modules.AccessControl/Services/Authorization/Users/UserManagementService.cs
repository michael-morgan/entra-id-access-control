using UI.Modules.AccessControl.Services.Graph;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Authorization.Users;

/// <summary>
/// Service implementation for managing users with business logic.
/// Orchestrates Graph API calls, repository calls, and ViewModel mapping.
/// </summary>
public class UserManagementService(
    GraphUserService graphUserService,
    IUserAttributeRepository userAttributeRepository,
    IPolicyRepository policyRepository,
    ILogger<UserManagementService> logger) : IUserManagementService
{
    private readonly GraphUserService _graphUserService = graphUserService;
    private readonly IUserAttributeRepository _userAttributeRepository = userAttributeRepository;
    private readonly IPolicyRepository _policyRepository = policyRepository;
    private readonly ILogger<UserManagementService> _logger = logger;

    public async Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId)
    {
        try
        {
            // Get user with groups from Graph API
            var userWithGroups = await _graphUserService.GetUserWithGroupsAsync(userId);
            if (userWithGroups == null)
            {
                return null;
            }

            // Get user attributes from database across all workstreams
            // We'll need to get all workstreams and query for each
            var workstreams = await _policyRepository.GetWorkstreamsAsync();
            var userAttributes = new List<Api.Modules.AccessControl.Persistence.Entities.Authorization.UserAttribute>();

            foreach (var workstream in workstreams)
            {
                var userAttribute = await _userAttributeRepository.GetByUserIdAndWorkstreamAsync(userId, workstream);
                if (userAttribute != null)
                {
                    userAttributes.Add(userAttribute);
                }
            }

            // Get role assignments (via group memberships) - single efficient query
            var groupIds = userWithGroups.Groups
                .Select(g => g.Id)
                .Where(id => !string.IsNullOrEmpty(id))
                .Cast<string>()
                .ToList();
            var roleAssignments = new List<string>();

            if (groupIds.Count > 0)
            {
                // Query all group-to-role mappings in a single database call
                var groupPolicies = await _policyRepository.GetBySubjectIdsAsync(groupIds, policyType: "g");

                foreach (var policy in groupPolicies)
                {
                    if (!string.IsNullOrWhiteSpace(policy.V1)) // V1 is the role
                    {
                        var roleDisplay = policy.WorkstreamId != null
                            ? $"{policy.V1} (via group, {policy.WorkstreamId})"
                            : $"{policy.V1} (via group)";
                        roleAssignments.Add(roleDisplay);
                    }
                }
            }

            // Create view model
            var viewModel = new UserDetailsViewModel
            {
                User = userWithGroups.User,
                Groups = userWithGroups.Groups,
                UserAttributes = userAttributes,
                RoleAssignments = roleAssignments.Distinct().ToList()
            };

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user details for {UserId}", userId);
            throw;
        }
    }

    public async Task<ManageRolesViewModel?> GetManageRolesDataAsync(string userId)
    {
        try
        {
            // Get user with groups
            var userWithGroups = await _graphUserService.GetUserWithGroupsAsync(userId);
            if (userWithGroups == null)
            {
                return null;
            }

            // Get all workstreams from policies
            var workstreams = await _policyRepository.GetWorkstreamsAsync();

            // Get all available roles per workstream
            var availableRoles = new Dictionary<string, List<string>>();
            foreach (var workstream in workstreams)
            {
                // Get permission policies (type "p") for this workstream
                var policies = await _policyRepository.SearchAsync(
                    workstream: workstream,
                    policyType: "p",
                    search: null
                );

                // Extract unique subjects (V0) which represent roles
                var roles = policies
                    .Where(p => !string.IsNullOrWhiteSpace(p.V0))
                    .Select(p => p.V0!)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();

                availableRoles[workstream] = roles;
            }

            // Get current role assignments for user's groups - single efficient query
            var groupIds = userWithGroups.Groups
                .Select(g => g.Id)
                .Where(id => !string.IsNullOrEmpty(id))
                .Cast<string>()
                .ToList();
            var currentRoleAssignments = new Dictionary<string, List<string>>();

            if (groupIds.Count > 0)
            {
                // Query all group-to-role mappings in a single database call
                var groupPolicies = await _policyRepository.GetBySubjectIdsAsync(groupIds, policyType: "g");

                foreach (var policy in groupPolicies)
                {
                    var ws = policy.WorkstreamId ?? "";
                    var role = policy.V1 ?? "";

                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        if (!currentRoleAssignments.ContainsKey(ws))
                        {
                            currentRoleAssignments[ws] = new List<string>();
                        }

                        if (!currentRoleAssignments[ws].Contains(role))
                        {
                            currentRoleAssignments[ws].Add(role);
                        }
                    }
                }
            }

            var viewModel = new ManageRolesViewModel
            {
                User = userWithGroups.User,
                Groups = userWithGroups.Groups,
                Workstreams = workstreams.ToList(),
                AvailableRoles = availableRoles,
                CurrentRoleAssignments = currentRoleAssignments
            };

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role management for user {UserId}", userId);
            throw;
        }
    }
}
