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
/// Controller for managing group attributes for ABAC.
/// </summary>
public class GroupAttributesController(
    IGroupAttributeManagementService groupAttributeManagementService,
    IGraphGroupService graphGroupService,
    ILogger<GroupAttributesController> logger) : Controller
{
    private readonly IGroupAttributeManagementService _groupAttributeManagementService = groupAttributeManagementService;
    private readonly IGraphGroupService _graphGroupService = graphGroupService;
    private readonly ILogger<GroupAttributesController> _logger = logger;

    // GET: GroupAttributes
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        // Service handles both fetching and syncing display names from Graph API
        var groupAttributes = await _groupAttributeManagementService.GetGroupAttributesAsync(selectedWorkstream, search);

        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(groupAttributes);
    }

    // GET: GroupAttributes/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var groupAttribute = await _groupAttributeManagementService.GetGroupAttributeByIdAsync(id.Value);
        if (groupAttribute == null) return NotFound();

        var viewModel = new GroupAttributeViewModel
        {
            Id = groupAttribute.Id,
            GroupId = groupAttribute.GroupId,
            WorkstreamId = groupAttribute.WorkstreamId,
            GroupName = groupAttribute.GroupName,
            IsActive = groupAttribute.IsActive,
            AttributesJson = groupAttribute.AttributesJson,
            CreatedAt = groupAttribute.CreatedAt,
            ModifiedAt = groupAttribute.ModifiedAt
        };

        return View(viewModel);
    }

    // GET: GroupAttributes/Create
    public async Task<IActionResult> Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;

        // Get all groups (groups are not workstream-specific)
        var groups = await _graphGroupService.GetAllGroupsAsync();
        ViewBag.Groups = groups.Select(g => new { Id = g.Id, DisplayName = g.DisplayName ?? g.Id }).ToList();

        return View();
    }

    // POST: GroupAttributes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupAttributeViewModel model)
    {
        if (ModelState.IsValid)
        {
            var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

            var (success, groupAttribute, errorMessage) = await _groupAttributeManagementService.CreateGroupAttributeAsync(
                model, selectedWorkstream);

            if (!success)
            {
                ModelState.AddModelError("GroupId", errorMessage!);
                ViewBag.SelectedWorkstream = selectedWorkstream;

                // Re-populate groups dropdown on error
                var groups = await _graphGroupService.GetAllGroupsAsync();
                ViewBag.Groups = groups.Select(g => new { Id = g.Id, DisplayName = g.DisplayName ?? g.Id }).ToList();

                return View(model);
            }

            _logger.LogInformation("Created group attributes for {GroupId} in workstream {Workstream}",
                groupAttribute!.GroupId, selectedWorkstream);

            TempData["SuccessMessage"] = "Group attributes created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Re-populate groups dropdown on validation error
        var selectedWorkstream2 = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream2;
        var groups2 = await _graphGroupService.GetAllGroupsAsync();
        ViewBag.Groups = groups2.Select(g => new { Id = g.Id, DisplayName = g.DisplayName ?? g.Id }).ToList();

        return View(model);
    }

    // GET: GroupAttributes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var groupAttribute = await _groupAttributeManagementService.GetGroupAttributeByIdAsync(id.Value);
        if (groupAttribute == null) return NotFound();

        var model = new GroupAttributeViewModel
        {
            Id = groupAttribute.Id,
            GroupId = groupAttribute.GroupId,
            WorkstreamId = groupAttribute.WorkstreamId,
            GroupName = groupAttribute.GroupName,
            IsActive = groupAttribute.IsActive,
            AttributesJson = groupAttribute.AttributesJson
        };

        ViewBag.SelectedWorkstream = groupAttribute.WorkstreamId;

        return View(model);
    }

    // POST: GroupAttributes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GroupAttributeViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var (success, errorMessage) = await _groupAttributeManagementService.UpdateGroupAttributeAsync(id, model);

            if (!success)
            {
                if (errorMessage == "Group attribute not found")
                {
                    return NotFound();
                }

                ModelState.AddModelError(string.Empty, errorMessage!);
                ViewBag.SelectedWorkstream = model.WorkstreamId;
                return View(model);
            }

            _logger.LogInformation("Updated group attributes for {GroupId}", model.GroupId);

            TempData["SuccessMessage"] = "Group attributes updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SelectedWorkstream = model.WorkstreamId;
        return View(model);
    }

    // GET: GroupAttributes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var groupAttribute = await _groupAttributeManagementService.GetGroupAttributeByIdAsync(id.Value);
        if (groupAttribute == null) return NotFound();

        var viewModel = new GroupAttributeViewModel
        {
            Id = groupAttribute.Id,
            GroupId = groupAttribute.GroupId,
            WorkstreamId = groupAttribute.WorkstreamId,
            GroupName = groupAttribute.GroupName,
            IsActive = groupAttribute.IsActive,
            AttributesJson = groupAttribute.AttributesJson,
            CreatedAt = groupAttribute.CreatedAt,
            ModifiedAt = groupAttribute.ModifiedAt
        };

        return View(viewModel);
    }

    // POST: GroupAttributes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var success = await _groupAttributeManagementService.DeleteGroupAttributeAsync(id);

        if (success)
        {
            _logger.LogWarning("Deleted group attributes {Id}", id);
            TempData["SuccessMessage"] = "Group attributes deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete group attributes. The record may not exist.";
        }

        return RedirectToAction(nameof(Index));
    }
}
