using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Controllers.Home;
using Microsoft.AspNetCore.Mvc;
using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Services.Attributes;

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

        var viewModel = new RoleAttributeViewModel
        {
            Id = roleAttribute.Id,
            RoleId = roleAttribute.RoleId,
            WorkstreamId = roleAttribute.WorkstreamId,
            RoleName = roleAttribute.RoleName,
            IsActive = roleAttribute.IsActive,
            AttributesJson = roleAttribute.AttributesJson,
            CreatedAt = roleAttribute.CreatedAt,
            ModifiedAt = roleAttribute.ModifiedAt
        };

        return View(viewModel);
    }

    // GET: RoleAttributes/Create
    public async Task<IActionResult> Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;

        // Get all roles for dropdown
        var roles = await _roleAttributeManagementService.GetRolesForWorkstreamAsync(selectedWorkstream);
        ViewBag.Roles = roles.Select(r => new { Id = r.RoleName, DisplayName = r.DisplayName }).ToList();

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
                RoleId = model.RoleId,
                WorkstreamId = selectedWorkstream,
                RoleName = model.RoleName,
                IsActive = model.IsActive,
                AttributesJson = model.AttributesJson
            };

            var (success, createdRoleAttribute, errorMessage) =
                await _roleAttributeManagementService.CreateRoleAttributeAsync(roleAttribute, selectedWorkstream);

            if (success)
            {
                _logger.LogInformation("Created role attributes for {RoleId} in workstream {Workstream}",
                    createdRoleAttribute!.RoleId, selectedWorkstream);
                TempData["SuccessMessage"] = "Role attributes created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("RoleId", errorMessage!);
        }

        // Re-populate roles on error
        var roles = await _roleAttributeManagementService.GetRolesForWorkstreamAsync(selectedWorkstream);
        ViewBag.Roles = roles.Select(r => new { Id = r.RoleName, DisplayName = r.DisplayName }).ToList();
        ViewBag.SelectedWorkstream = selectedWorkstream;

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
            RoleId = roleAttribute.RoleId,
            WorkstreamId = roleAttribute.WorkstreamId,
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
                RoleId = model.RoleId,
                WorkstreamId = model.WorkstreamId,
                IsActive = model.IsActive,
                AttributesJson = model.AttributesJson
            };

            var (success, errorMessage) = await _roleAttributeManagementService.UpdateRoleAttributeAsync(id, roleAttribute);

            if (success)
            {
                _logger.LogInformation("Updated role attributes for {RoleId} in workstream {Workstream}",
                    roleAttribute.RoleId, roleAttribute.WorkstreamId);
                TempData["SuccessMessage"] = "Role attributes updated successfully.";
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

        var viewModel = new RoleAttributeViewModel
        {
            Id = roleAttribute.Id,
            RoleId = roleAttribute.RoleId,
            WorkstreamId = roleAttribute.WorkstreamId,
            RoleName = roleAttribute.RoleName,
            IsActive = roleAttribute.IsActive,
            AttributesJson = roleAttribute.AttributesJson,
            CreatedAt = roleAttribute.CreatedAt,
            ModifiedAt = roleAttribute.ModifiedAt
        };

        return View(viewModel);
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
                _logger.LogWarning("Deleted role attributes for {RoleId}",
                    roleAttribute.RoleId);
                TempData["SuccessMessage"] = "Role attributes deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete role attributes. The record may not exist.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Role attribute not found.";
        }

        return RedirectToAction(nameof(Index));
    }
}
