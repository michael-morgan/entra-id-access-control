using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Services.Groups;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing Entra ID groups in the admin UI.
/// Allows viewing and enriching group display names and descriptions.
/// </summary>
[Authorize]
public class GroupsController(
    IGroupManagementService groupManagementService,
    ILogger<GroupsController> logger) : Controller
{
    private readonly IGroupManagementService _groupManagementService = groupManagementService;
    private readonly ILogger<GroupsController> _logger = logger;

    /// <summary>
    /// Display list of all groups.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var groups = await _groupManagementService.GetAllGroupsAsync();
            return View(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading groups list");
            TempData["Error"] = "Failed to load groups. Please try again.";
            return View(new List<GroupListItemViewModel>());
        }
    }

    /// <summary>
    /// Display detailed information about a specific group.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        try
        {
            var group = await _groupManagementService.GetGroupDetailsAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            return View(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading group details for {GroupId}", id);
            TempData["Error"] = "Failed to load group details. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Display edit form for enriching group information.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        try
        {
            var group = await _groupManagementService.GetGroupDetailsAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            var viewModel = new GroupEditViewModel
            {
                GroupId = group.GroupId,
                DisplayName = group.DisplayName,
                Description = group.Description,
                Source = group.Source
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading group edit form for {GroupId}", id);
            TempData["Error"] = "Failed to load edit form. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Process group edit form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GroupEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var success = await _groupManagementService.UpdateGroupAsync(
                model.GroupId,
                model.DisplayName,
                model.Description);

            if (!success)
            {
                TempData["Error"] = "Group not found.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"Group '{model.DisplayName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.GroupId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", model.GroupId);
            TempData["Error"] = "Failed to update group. Please try again.";
            return View(model);
        }
    }

    /// <summary>
    /// Display stale user-group associations (not seen in JWT recently).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> StaleAssociations(int? daysThreshold)
    {
        try
        {
            // Default to 30 days if not specified
            var threshold = daysThreshold ?? 30;
            ViewBag.DaysThreshold = threshold;

            var staleAssociations = await _groupManagementService.GetStaleAssociationsAsync(threshold);
            return View(staleAssociations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stale associations");
            TempData["Error"] = "Failed to load stale associations. Please try again.";
            return View(new List<StaleAssociationViewModel>());
        }
    }

    /// <summary>
    /// Keep a stale association (convert from JWT to Manual).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KeepStaleAssociation(int id, int? daysThreshold)
    {
        try
        {
            var success = await _groupManagementService.KeepStaleAssociationAsync(id);
            if (success)
            {
                TempData["Success"] = "Association marked as Manual and will be kept.";
            }
            else
            {
                TempData["Error"] = "Association not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error keeping stale association {AssociationId}", id);
            TempData["Error"] = "Failed to update association. Please try again.";
        }

        return RedirectToAction(nameof(StaleAssociations), new { daysThreshold });
    }

    /// <summary>
    /// Remove a stale association.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStaleAssociation(int id, int? daysThreshold)
    {
        try
        {
            var success = await _groupManagementService.RemoveStaleAssociationAsync(id);
            if (success)
            {
                TempData["Success"] = "Association removed successfully.";
            }
            else
            {
                TempData["Error"] = "Association not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing stale association {AssociationId}", id);
            TempData["Error"] = "Failed to remove association. Please try again.";
        }

        return RedirectToAction(nameof(StaleAssociations), new { daysThreshold });
    }
}
