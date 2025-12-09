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
    ILogger<GroupAttributesController> logger) : Controller
{
    private readonly IGroupAttributeManagementService _groupAttributeManagementService = groupAttributeManagementService;
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
            AttributesJson = groupAttribute.AttributesJson
        };

        return View(viewModel);
    }

    // GET: GroupAttributes/Create
    public IActionResult Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;
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
                return View(model);
            }

            _logger.LogInformation("Created group attributes for {GroupId} in workstream {Workstream}",
                groupAttribute!.GroupId, selectedWorkstream);

            return RedirectToAction(nameof(Index));
        }
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
            AttributesJson = groupAttribute.AttributesJson
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
        }

        return RedirectToAction(nameof(Index));
    }
}
