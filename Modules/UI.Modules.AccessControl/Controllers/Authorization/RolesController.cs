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
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers.Authorization;

/// <summary>
/// Controller for managing Casbin roles.
/// </summary>
public class RolesController(
    IRoleManagementService roleManagementService,
    ILogger<RolesController> logger) : Controller
{
    private readonly IRoleManagementService _roleManagementService = roleManagementService;
    private readonly ILogger<RolesController> _logger = logger;

    // GET: Roles
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var roles = await _roleManagementService.GetRolesAsync(selectedWorkstream, search);

        var viewModels = roles.Select(r => new RoleViewModel
        {
            Id = r.Id,
            RoleName = r.RoleName,
            WorkstreamId = r.WorkstreamId,
            DisplayName = r.DisplayName,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            CreatedBy = r.CreatedBy,
            ModifiedAt = r.ModifiedAt,
            ModifiedBy = r.ModifiedBy
        }).ToList();

        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(viewModels);
    }

    // GET: Roles/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var role = await _roleManagementService.GetRoleByIdAsync(id.Value);
        if (role == null) return NotFound();

        var viewModel = new RoleViewModel
        {
            Id = role.Id,
            RoleName = role.RoleName,
            WorkstreamId = role.WorkstreamId,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive,
            CreatedAt = role.CreatedAt,
            CreatedBy = role.CreatedBy,
            ModifiedAt = role.ModifiedAt,
            ModifiedBy = role.ModifiedBy
        };

        return View(viewModel);
    }

    // GET: Roles/Create
    public IActionResult Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View();
    }

    // POST: Roles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleViewModel model)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        if (ModelState.IsValid)
        {
            var createdBy = User.Identity?.Name ?? "System";
            var (success, role, errorMessage) = await _roleManagementService.CreateRoleAsync(
                model, selectedWorkstream, createdBy);

            if (!success)
            {
                ModelState.AddModelError(nameof(model.RoleName), errorMessage!);
                ViewBag.SelectedWorkstream = selectedWorkstream;
                return View(model);
            }

            _logger.LogInformation("Created role {RoleName} in workstream {Workstream}",
                role!.RoleName, role.WorkstreamId);

            TempData["SuccessMessage"] = "Role created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View(model);
    }

    // GET: Roles/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var role = await _roleManagementService.GetRoleByIdAsync(id.Value);
        if (role == null) return NotFound();

        var viewModel = new RoleViewModel
        {
            Id = role.Id,
            RoleName = role.RoleName,
            WorkstreamId = role.WorkstreamId,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive,
            CreatedAt = role.CreatedAt,
            CreatedBy = role.CreatedBy,
            ModifiedAt = role.ModifiedAt,
            ModifiedBy = role.ModifiedBy
        };

        ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        return View(viewModel);
    }

    // POST: Roles/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoleViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var modifiedBy = User.Identity?.Name ?? "System";
                var (success, errorMessage) = await _roleManagementService.UpdateRoleAsync(id, model, modifiedBy);

                if (!success)
                {
                    if (errorMessage == "Role not found")
                    {
                        return NotFound();
                    }

                    ModelState.AddModelError(nameof(model.RoleName), errorMessage!);
                    ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
                    return View(model);
                }

                _logger.LogInformation("Updated role {RoleName} in workstream {Workstream}",
                    model.RoleName, model.WorkstreamId);

                TempData["SuccessMessage"] = "Role updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _roleManagementService.RoleExistsAsync(model.Id))
                    return NotFound();
                throw;
            }
        }

        ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        return View(model);
    }

    // GET: Roles/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var role = await _roleManagementService.GetRoleByIdAsync(id.Value);
        if (role == null) return NotFound();

        var viewModel = new RoleViewModel
        {
            Id = role.Id,
            RoleName = role.RoleName,
            WorkstreamId = role.WorkstreamId,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            IsActive = role.IsActive,
            CreatedAt = role.CreatedAt,
            CreatedBy = role.CreatedBy,
            ModifiedAt = role.ModifiedAt,
            ModifiedBy = role.ModifiedBy
        };

        return View(viewModel);
    }

    // POST: Roles/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (success, errorMessage) = await _roleManagementService.DeleteRoleAsync(id);

        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Index));
        }

        _logger.LogWarning("Deleted role {Id}", id);
        TempData["SuccessMessage"] = "Role deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}
