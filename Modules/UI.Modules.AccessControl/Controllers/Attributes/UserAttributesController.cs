using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Controllers.Home;
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
using Microsoft.AspNetCore.Mvc;

namespace UI.Modules.AccessControl.Controllers.Attributes;

/// <summary>
/// Controller for managing user attributes for ABAC.
/// </summary>
public class UserAttributesController(
    IUserAttributeManagementService userAttributeManagementService,
    ILogger<UserAttributesController> logger) : Controller
{
    private readonly IUserAttributeManagementService _userAttributeManagementService = userAttributeManagementService;
    private readonly ILogger<UserAttributesController> _logger = logger;

    // GET: UserAttributes
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var (userAttributes, userDisplayNames) =
            await _userAttributeManagementService.GetUserAttributesWithDisplayNamesAsync(selectedWorkstream, search);

        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;
        ViewBag.UserDisplayNames = userDisplayNames;

        return View(userAttributes);
    }

    // GET: UserAttributes/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var result = await _userAttributeManagementService.GetUserAttributeByIdWithDisplayNameAsync(id.Value);
        if (result == null)
        {
            return NotFound();
        }

        var (userAttribute, userDisplayName) = result.Value;
        ViewBag.UserDisplayName = userDisplayName;

        return View(userAttribute);
    }

    // GET: UserAttributes/Create
    public IActionResult Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View();
    }

    // POST: UserAttributes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserAttributeViewModel model)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        if (ModelState.IsValid)
        {
            var (success, userAttribute, errorMessage) =
                await _userAttributeManagementService.CreateUserAttributeAsync(model, selectedWorkstream);

            if (!success)
            {
                ModelState.AddModelError(nameof(model.UserId), errorMessage!);
                ViewBag.SelectedWorkstream = selectedWorkstream;
                return View(model);
            }

            _logger.LogInformation("Created user attributes for {UserId} in workstream {Workstream}",
                userAttribute!.UserId, selectedWorkstream);

            TempData["SuccessMessage"] = "User attributes created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View(model);
    }

    // GET: UserAttributes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var result = await _userAttributeManagementService.GetUserAttributeByIdWithDisplayNameAsync(id.Value);
        if (result == null)
        {
            return NotFound();
        }

        var (userAttribute, userDisplayName) = result.Value;

        var model = new UserAttributeViewModel
        {
            Id = userAttribute!.Id,
            UserId = userAttribute.UserId,
            WorkstreamId = userAttribute.WorkstreamId,
            AttributesJson = userAttribute.AttributesJson
        };

        ViewBag.SelectedWorkstream = userAttribute.WorkstreamId;
        ViewBag.UserDisplayName = userDisplayName;

        return View(model);
    }

    // POST: UserAttributes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserAttributeViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var (success, errorMessage) = await _userAttributeManagementService.UpdateUserAttributeAsync(id, model);

            if (!success)
            {
                if (errorMessage == "User attribute not found")
                {
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, errorMessage!);
                return View(model);
            }

            _logger.LogInformation("Updated user attributes for ID {Id}", id);

            TempData["SuccessMessage"] = "User attributes updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: UserAttributes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var result = await _userAttributeManagementService.GetUserAttributeByIdWithDisplayNameAsync(id.Value);
        if (result == null)
        {
            return NotFound();
        }

        var (userAttribute, userDisplayName) = result.Value;
        ViewBag.UserDisplayName = userDisplayName;

        return View(userAttribute);
    }

    // POST: UserAttributes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var success = await _userAttributeManagementService.DeleteUserAttributeAsync(id);

        if (success)
        {
            _logger.LogWarning("Deleted user attributes for ID {Id}", id);
            TempData["SuccessMessage"] = "User attributes deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
