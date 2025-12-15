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
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UI.Modules.AccessControl.Controllers.AbacRules;

public class AbacRuleGroupsController(
    IAbacRuleGroupManagementService ruleGroupManagementService,
    ILogger<AbacRuleGroupsController> logger) : Controller
{
    private readonly IAbacRuleGroupManagementService _ruleGroupManagementService = ruleGroupManagementService;
    private readonly ILogger<AbacRuleGroupsController> _logger = logger;

    // GET: AbacRuleGroups
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var viewModels = await _ruleGroupManagementService.GetRuleGroupsAsync(selectedWorkstream, search);

        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(viewModels);
    }

    // GET: AbacRuleGroups/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var group = await _ruleGroupManagementService.GetRuleGroupByIdAsync(id.Value);
        if (group == null)
        {
            return NotFound();
        }

        return View(group);
    }

    // GET: AbacRuleGroups/Create
    public async Task<IActionResult> Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        await PopulateParentGroupsDropdown(selectedWorkstream);
        await PopulateResourcesDropdown(selectedWorkstream);
        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View();
    }

    // POST: AbacRuleGroups/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AbacRuleGroupViewModel model)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        if (ModelState.IsValid)
        {
            var (success, ruleGroup, errorMessage) =
                await _ruleGroupManagementService.CreateRuleGroupAsync(model, selectedWorkstream);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage!);
                await PopulateParentGroupsDropdown(selectedWorkstream, model.ParentGroupId);
                await PopulateResourcesDropdown(selectedWorkstream);
                ViewBag.SelectedWorkstream = selectedWorkstream;
                return View(model);
            }

            _logger.LogInformation("Created rule group {GroupName} for workstream {Workstream}",
                model.GroupName, selectedWorkstream);

            TempData["SuccessMessage"] = "Rule group created successfully.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateParentGroupsDropdown(selectedWorkstream, model.ParentGroupId);
        await PopulateResourcesDropdown(selectedWorkstream);
        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View(model);
    }

    // GET: AbacRuleGroups/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var model = await _ruleGroupManagementService.GetRuleGroupViewModelByIdAsync(id.Value);
        if (model == null)
        {
            return NotFound();
        }

        await PopulateParentGroupsDropdown(model.WorkstreamId, model.ParentGroupId, model.Id);
        await PopulateResourcesDropdown(model.WorkstreamId);
        return View(model);
    }

    // POST: AbacRuleGroups/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AbacRuleGroupViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var (success, errorMessage) = await _ruleGroupManagementService.UpdateRuleGroupAsync(id, model);

            if (!success)
            {
                if (errorMessage == "Rule group not found")
                {
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, errorMessage!);
                await PopulateParentGroupsDropdown(model.WorkstreamId, model.ParentGroupId, model.Id);
                await PopulateResourcesDropdown(model.WorkstreamId);
                return View(model);
            }

            _logger.LogInformation("Updated rule group {GroupName}", model.GroupName);

            TempData["SuccessMessage"] = "Rule group updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateParentGroupsDropdown(model.WorkstreamId, model.ParentGroupId, model.Id);
        await PopulateResourcesDropdown(model.WorkstreamId);
        return View(model);
    }

    // GET: AbacRuleGroups/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var group = await _ruleGroupManagementService.GetRuleGroupByIdAsync(id.Value);
        if (group == null)
        {
            return NotFound();
        }

        return View(group);
    }

    // POST: AbacRuleGroups/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (success, errorMessage) = await _ruleGroupManagementService.DeleteRuleGroupAsync(id);

        if (!success)
        {
            if (errorMessage == "Rule group not found")
            {
                return RedirectToAction(nameof(Index));
            }

            // Load the group for redisplay
            var group = await _ruleGroupManagementService.GetRuleGroupByIdAsync(id);
            if (group != null)
            {
                ModelState.AddModelError("", errorMessage!);
                return View(group);
            }

            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Deleted rule group ID {Id}", id);
        TempData["SuccessMessage"] = "Rule group deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateParentGroupsDropdown(string workstreamId, int? selectedValue = null, int? excludeId = null)
    {
        var groups = await _ruleGroupManagementService.GetParentGroupOptionsAsync(workstreamId, excludeId);
        var groupList = groups.Select(g => new { g.Id, g.GroupName }).ToList();
        ViewBag.ParentGroups = new SelectList(groupList, "Id", "GroupName", selectedValue);
    }

    private async Task PopulateResourcesDropdown(string workstreamId)
    {
        var resources = await _ruleGroupManagementService.GetAvailableResourcesAsync(workstreamId);
        ViewBag.AvailableResources = resources.ToList();
    }
}
