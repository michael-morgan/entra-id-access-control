using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Controllers.Home;
using Microsoft.AspNetCore.Mvc;
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

namespace UI.Modules.AccessControl.Controllers.Attributes;

/// <summary>
/// Controller for managing role attributes for ABAC.
/// </summary>
public class RoleAttributesController(
    IRoleAttributeManagementService roleAttributeManagementService,
    ILogger<RoleAttributesController> logger) : Controller
{
    private readonly IRoleAttributeManagementService _roleAttributeManagementService = roleAttributeManagementService;
    private readonly ILogger<RoleAttributesController> _logger = logger;

    // GET: RoleAttributes
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var roleAttributes = await _roleAttributeManagementService.GetRoleAttributesAsync(selectedWorkstream, search);

        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(roleAttributes);
    }

    // GET: RoleAttributes/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roleAttribute = await _roleAttributeManagementService.GetRoleAttributeByIdAsync(id.Value);
        if (roleAttribute == null)
        {
            return NotFound();
        }

        return View(roleAttribute);
    }

    // GET: RoleAttributes/Create
    public IActionResult Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View();
    }

    // POST: RoleAttributes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleAttributeViewModel model)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        if (ModelState.IsValid)
        {
            var roleAttribute = new RoleAttribute
            {
                AppRoleId = model.AppRoleId,
                RoleValue = model.RoleValue,
                WorkstreamId = selectedWorkstream,
                RoleDisplayName = model.RoleDisplayName,
                IsActive = model.IsActive,
                AttributesJson = model.AttributesJson
            };

            var (success, createdRoleAttribute, errorMessage) =
                await _roleAttributeManagementService.CreateRoleAttributeAsync(roleAttribute, selectedWorkstream);

            if (success)
            {
                _logger.LogInformation("Created role attributes for {AppRoleId} ({RoleValue}) in workstream {Workstream}",
                    createdRoleAttribute!.AppRoleId, createdRoleAttribute.RoleValue, selectedWorkstream);
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("AppRoleId", errorMessage!);
        }

        return View(model);
    }

    // GET: RoleAttributes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roleAttribute = await _roleAttributeManagementService.GetRoleAttributeByIdAsync(id.Value);
        if (roleAttribute == null)
        {
            return NotFound();
        }

        var model = new RoleAttributeViewModel
        {
            Id = roleAttribute.Id,
            AppRoleId = roleAttribute.AppRoleId,
            RoleValue = roleAttribute.RoleValue,
            WorkstreamId = roleAttribute.WorkstreamId,
            RoleDisplayName = roleAttribute.RoleDisplayName,
            IsActive = roleAttribute.IsActive,
            AttributesJson = roleAttribute.AttributesJson
        };

        ViewBag.SelectedWorkstream = roleAttribute.WorkstreamId;

        return View(model);
    }

    // POST: RoleAttributes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoleAttributeViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var roleAttribute = new RoleAttribute
            {
                Id = model.Id,
                AppRoleId = model.AppRoleId,
                RoleValue = model.RoleValue,
                WorkstreamId = model.WorkstreamId,
                RoleDisplayName = model.RoleDisplayName,
                IsActive = model.IsActive,
                AttributesJson = model.AttributesJson
            };

            var (success, errorMessage) = await _roleAttributeManagementService.UpdateRoleAttributeAsync(id, roleAttribute);

            if (success)
            {
                _logger.LogInformation("Updated role attributes for {AppRoleId} ({RoleValue}) in workstream {Workstream}",
                    roleAttribute.AppRoleId, roleAttribute.RoleValue, roleAttribute.WorkstreamId);
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, errorMessage!);
        }

        return View(model);
    }

    // GET: RoleAttributes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roleAttribute = await _roleAttributeManagementService.GetRoleAttributeByIdAsync(id.Value);
        if (roleAttribute == null)
        {
            return NotFound();
        }

        return View(roleAttribute);
    }

    // POST: RoleAttributes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var roleAttribute = await _roleAttributeManagementService.GetRoleAttributeByIdAsync(id);
        if (roleAttribute != null)
        {
            var success = await _roleAttributeManagementService.DeleteRoleAttributeAsync(id);
            if (success)
            {
                _logger.LogWarning("Deleted role attributes for {AppRoleId} ({RoleValue})",
                    roleAttribute.AppRoleId, roleAttribute.RoleValue);
            }
        }

        return RedirectToAction(nameof(Index));
    }
}
