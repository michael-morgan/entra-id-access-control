using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
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

namespace UI.Modules.AccessControl.Controllers.Users;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
public class UsersController(
    GraphUserService graphUserService,
    IUserManagementService userManagementService,
    ILogger<UsersController> logger) : Controller
{
    private readonly GraphUserService _graphUserService = graphUserService;
    private readonly IUserManagementService _userManagementService = userManagementService;
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
            var viewModel = await _userManagementService.GetUserDetailsAsync(id);
            if (viewModel == null)
            {
                return NotFound();
            }

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
            var viewModel = await _userManagementService.GetManageRolesDataAsync(id);
            if (viewModel == null)
            {
                return NotFound();
            }

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
