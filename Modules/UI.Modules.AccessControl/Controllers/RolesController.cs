using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing Casbin roles.
/// </summary>
public class RolesController : Controller
{
    private readonly AccessControlDbContext _context;
    private readonly ILogger<RolesController> _logger;

    public RolesController(AccessControlDbContext context, ILogger<RolesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Roles
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.CasbinRoles
            .Where(r => r.WorkstreamId == selectedWorkstream || r.WorkstreamId == "*");

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.RoleName.Contains(search) ||
                                   r.DisplayName.Contains(search) ||
                                   (r.Description != null && r.Description.Contains(search)));
        }

        var roles = await query
            .OrderBy(r => r.WorkstreamId)
            .ThenBy(r => r.RoleName)
            .ToListAsync();

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

        var role = await _context.CasbinRoles
            .FirstOrDefaultAsync(m => m.Id == id);

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
            // Check for duplicate role names in the same workstream
            var existingRole = await _context.CasbinRoles
                .AnyAsync(r => r.RoleName == model.RoleName && r.WorkstreamId == selectedWorkstream);

            if (existingRole)
            {
                ModelState.AddModelError(nameof(model.RoleName),
                    $"A role with the name '{model.RoleName}' already exists in workstream '{selectedWorkstream}'.");
                ViewBag.SelectedWorkstream = selectedWorkstream;
                return View(model);
            }

            var role = new CasbinRole
            {
                RoleName = model.RoleName,
                WorkstreamId = selectedWorkstream,
                DisplayName = model.DisplayName,
                Description = model.Description,
                IsSystemRole = model.IsSystemRole,
                IsActive = model.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Add(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created role {RoleName} in workstream {Workstream}",
                role.RoleName, role.WorkstreamId);

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

        var role = await _context.CasbinRoles.FindAsync(id);
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
                var role = await _context.CasbinRoles.FindAsync(id);
                if (role == null) return NotFound();

                // Check for duplicate role names in the same workstream (excluding current role)
                var existingRole = await _context.CasbinRoles
                    .AnyAsync(r => r.RoleName == model.RoleName &&
                                 r.WorkstreamId == role.WorkstreamId &&
                                 r.Id != id);

                if (existingRole)
                {
                    ModelState.AddModelError(nameof(model.RoleName),
                        $"A role with the name '{model.RoleName}' already exists in workstream '{role.WorkstreamId}'.");
                    ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
                    return View(model);
                }

                role.RoleName = model.RoleName;
                role.DisplayName = model.DisplayName;
                role.Description = model.Description;
                role.IsSystemRole = model.IsSystemRole;
                role.IsActive = model.IsActive;
                role.ModifiedAt = DateTimeOffset.UtcNow;
                role.ModifiedBy = User.Identity?.Name ?? "System";

                _context.Update(role);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated role {RoleName} in workstream {Workstream}",
                    role.RoleName, role.WorkstreamId);

                TempData["SuccessMessage"] = "Role updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await RoleExists(model.Id))
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

        var role = await _context.CasbinRoles
            .FirstOrDefaultAsync(m => m.Id == id);

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
        var role = await _context.CasbinRoles.FindAsync(id);
        if (role != null)
        {
            if (role.IsSystemRole)
            {
                TempData["ErrorMessage"] = "Cannot delete system roles.";
                return RedirectToAction(nameof(Index));
            }

            // Check if role is referenced in policies
            var isReferenced = await _context.CasbinPolicies
                .AnyAsync(p => p.V0 == role.RoleName || p.V1 == role.RoleName);

            if (isReferenced)
            {
                TempData["ErrorMessage"] = $"Cannot delete role '{role.RoleName}' because it is referenced in policies. Remove those references first.";
                return RedirectToAction(nameof(Index));
            }

            _context.CasbinRoles.Remove(role);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Deleted role {RoleName} from workstream {Workstream}",
                role.RoleName, role.WorkstreamId);

            TempData["SuccessMessage"] = "Role deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> RoleExists(int id)
    {
        return await _context.CasbinRoles.AnyAsync(e => e.Id == id);
    }
}
