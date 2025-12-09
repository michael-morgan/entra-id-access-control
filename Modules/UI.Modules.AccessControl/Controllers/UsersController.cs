using Api.Modules.AccessControl.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Services;

namespace UI.Modules.AccessControl.Controllers;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
public class UsersController(
    GraphUserService graphUserService,
    GraphGroupService graphGroupService,
    AccessControlDbContext context,
    ILogger<UsersController> logger) : Controller
{
    private readonly GraphUserService _graphUserService = graphUserService;
    private readonly GraphGroupService _graphGroupService = graphGroupService;
    private readonly AccessControlDbContext _context = context;
    private readonly ILogger<UsersController> _logger = logger;

    // GET: Users
    public async Task<IActionResult> Index(string? searchTerm)
    {
        // MicrosoftIdentityWebChallengeUserException is handled globally by MsalUiRequiredExceptionFilter
        // Don't catch it here - let it bubble up to the filter
        var users = string.IsNullOrWhiteSpace(searchTerm)
            ? await _graphUserService.GetAllUsersAsync()
            : await _graphUserService.SearchUsersAsync(searchTerm);

        ViewBag.SearchTerm = searchTerm;
        return View(users);
    }

    // GET: Users/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            // Get user with groups from Graph API
            var userWithGroups = await _graphUserService.GetUserWithGroupsAsync(id);
            if (userWithGroups == null)
            {
                return NotFound();
            }

            // Get user attributes from database
            var userAttributes = await _context.UserAttributes
                .Where(ua => ua.UserId == id)
                .OrderBy(ua => ua.WorkstreamId)
                .ToListAsync();

            // Get role assignments (via group memberships)
            var groupIds = userWithGroups.Groups.Select(g => g.Id).ToList();
            var roleAssignments = await _context.CasbinPolicies
                .Where(p => p.PolicyType == "g" && groupIds.Contains(p.V0!))
                .Select(p => new { GroupId = p.V0, Role = p.V1, Workstream = p.V2 })
                .ToListAsync();

            // Create view model
            var viewModel = new UserDetailsViewModel
            {
                User = userWithGroups.User,
                Groups = userWithGroups.Groups,
                UserAttributes = userAttributes,
                RoleAssignments = [.. roleAssignments
                    .Select(ra => $"{ra.Role} (via group, {ra.Workstream})")
                    .Distinct()]
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user details for {UserId}", id);
            TempData["Error"] = "Failed to retrieve user details. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Users/ManageRoles/5
    public async Task<IActionResult> ManageRoles(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            // Get user with groups
            var userWithGroups = await _graphUserService.GetUserWithGroupsAsync(id);
            if (userWithGroups == null)
            {
                return NotFound();
            }

            // Get all workstreams from policies
            var workstreams = await _context.CasbinPolicies
                .Where(p => !string.IsNullOrEmpty(p.WorkstreamId))
                .Select(p => p.WorkstreamId!)
                .Distinct()
                .OrderBy(w => w)
                .ToListAsync();

            // Get all available roles per workstream
            var availableRoles = new Dictionary<string, List<string>>();
            foreach (var workstream in workstreams)
            {
                var roles = await _context.CasbinPolicies
                    .Where(p => p.PolicyType == "p" && p.WorkstreamId == workstream)
                    .Select(p => p.V0!)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToListAsync();

                availableRoles[workstream] = roles;
            }

            // Get current role assignments for user's groups
            var groupIds = userWithGroups.Groups.Select(g => g.Id).ToList();
            var currentRoles = await _context.CasbinPolicies
                .Where(p => p.PolicyType == "g" && groupIds.Contains(p.V0!))
                .Select(p => new { GroupId = p.V0, Role = p.V1, Workstream = p.WorkstreamId })
                .ToListAsync();

            var viewModel = new ManageRolesViewModel
            {
                User = userWithGroups.User,
                Groups = userWithGroups.Groups,
                Workstreams = workstreams,
                AvailableRoles = availableRoles,
                CurrentRoleAssignments = currentRoles
                    .GroupBy(r => r.Workstream)
                    .ToDictionary(
                        g => g.Key ?? "",
                        g => g.Select(r => r.Role ?? "").ToList()
                    )
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role management for user {UserId}", id);
            TempData["Error"] = "Failed to load role management. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // Note: Actual role assignment/removal would require updating Casbin policies
    // This is a read-only view showing the current state
    // Future enhancement: Add POST actions to modify group-to-role mappings
}
