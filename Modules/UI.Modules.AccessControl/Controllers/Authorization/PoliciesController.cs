using Microsoft.AspNetCore.Mvc;
using UI.Modules.AccessControl.Controllers.Home;
using Microsoft.EntityFrameworkCore;
using UI.Modules.AccessControl.Models;
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

namespace UI.Modules.AccessControl.Controllers.Authorization;

/// <summary>
/// Controller for managing Casbin policies.
/// </summary>
public class PoliciesController(
    IPolicyManagementService policyManagementService,
    IGraphGroupService graphGroupService,
    IGraphUserService graphUserService,
    ILogger<PoliciesController> logger) : Controller
{
    private readonly IPolicyManagementService _policyManagementService = policyManagementService;
    private readonly IGraphGroupService _graphGroupService = graphGroupService;
    private readonly IGraphUserService _graphUserService = graphUserService;
    private readonly ILogger<PoliciesController> _logger = logger;

    // GET: Policies
    public async Task<IActionResult> Index(string? policyType = null, string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        var (policies, displayNames, policyTypes) =
            await _policyManagementService.GetPoliciesWithDisplayNamesAsync(selectedWorkstream, policyType, search);

        ViewBag.PolicyType = policyType;
        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;
        ViewBag.DisplayNames = displayNames;
        ViewBag.PolicyTypes = policyTypes;

        return View(policies);
    }

    // GET: Policies/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var result = await _policyManagementService.GetPolicyByIdWithDisplayNamesAsync(id.Value);
        if (result == null)
        {
            return NotFound();
        }

        var (policy, subjectNames) = result.Value;
        ViewBag.SubjectNames = subjectNames;

        return View(policy);
    }

    // GET: Policies/Create
    public async Task<IActionResult> Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        // Load available role names for the current workstream
        var roleNames = await _policyManagementService.GetAvailableRoleNamesAsync(selectedWorkstream);

        // Load available resources for the current workstream (including global)
        var resources = await _policyManagementService.GetAvailableResourcePatternsAsync(selectedWorkstream);

        // Load available workstreams
        var workstreams = await _policyManagementService.GetAvailableWorkstreamsAsync();

        // Load groups and users for subject dropdown
        var groups = await _graphGroupService.GetAllGroupsAsync();
        var users = await _graphUserService.GetAllUsersAsync();

        ViewBag.AvailableRoles = roleNames;
        ViewBag.AvailableResources = resources;
        ViewBag.AvailableWorkstreams = workstreams;
        ViewBag.SelectedWorkstream = selectedWorkstream;
        ViewBag.Groups = groups;
        ViewBag.Users = users;

        return View();
    }

    // POST: Policies/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PolicyViewModel model)
    {
        if (ModelState.IsValid)
        {
            var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
            var createdBy = User.Identity?.Name ?? "System";

            var policy = await _policyManagementService.CreatePolicyAsync(model, selectedWorkstream, createdBy);

            _logger.LogInformation("Created new policy in workstream {Workstream}: {PolicyType} {V0} {V1} {V2}",
                selectedWorkstream, policy.PolicyType, policy.V0, policy.V1, policy.V2);

            TempData["SuccessMessage"] = "Policy created successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Policies/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var result = await _policyManagementService.GetPolicyByIdWithDisplayNamesAsync(id.Value);
        if (result == null)
        {
            return NotFound();
        }

        var (policy, _) = result.Value;

        // Additional safety check (should never be null if result is not null, but satisfies compiler)
        if (policy == null)
        {
            return NotFound();
        }

        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        // Load available role names for the current workstream
        var roleNames = await _policyManagementService.GetAvailableRoleNamesAsync(selectedWorkstream);

        // Load available resources for the current workstream (including global)
        var resources = await _policyManagementService.GetAvailableResourcePatternsAsync(selectedWorkstream);

        // Load available workstreams
        var workstreams = await _policyManagementService.GetAvailableWorkstreamsAsync();

        // Load groups and users for subject dropdown
        var groups = await _graphGroupService.GetAllGroupsAsync();
        var users = await _graphUserService.GetAllUsersAsync();

        ViewBag.AvailableRoles = roleNames;
        ViewBag.AvailableResources = resources;
        ViewBag.AvailableWorkstreams = workstreams;
        ViewBag.SelectedWorkstream = selectedWorkstream;
        ViewBag.Groups = groups;
        ViewBag.Users = users;

        var model = new PolicyViewModel
        {
            Id = policy.Id,
            PolicyType = policy.PolicyType,
            V0 = policy.V0,
            V1 = policy.V1,
            V2 = policy.V2,
            V3 = policy.V3,
            V4 = policy.V4,
            V5 = policy.V5,
            IsActive = policy.IsActive
        };

        return View(model);
    }

    // POST: Policies/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PolicyViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var modifiedBy = User.Identity?.Name ?? "System";
                var success = await _policyManagementService.UpdatePolicyAsync(id, model, modifiedBy);

                if (!success)
                {
                    return NotFound();
                }

                _logger.LogInformation("Updated policy {Id}: {PolicyType} {V0} {V1} {V2}",
                    id, model.PolicyType, model.V0, model.V1, model.V2);

                TempData["SuccessMessage"] = "Policy updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _policyManagementService.PolicyExistsAsync(model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Policies/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var result = await _policyManagementService.GetPolicyByIdWithDisplayNamesAsync(id.Value);
        if (result == null)
        {
            return NotFound();
        }

        var (policy, subjectNames) = result.Value;
        ViewBag.SubjectNames = subjectNames;

        return View(policy);
    }

    // POST: Policies/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var success = await _policyManagementService.DeletePolicyAsync(id);
        if (success)
        {
            _logger.LogWarning("Deleted policy {Id}", id);
            TempData["SuccessMessage"] = "Policy deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete policy.";
        }

        return RedirectToAction(nameof(Index));
    }
}
