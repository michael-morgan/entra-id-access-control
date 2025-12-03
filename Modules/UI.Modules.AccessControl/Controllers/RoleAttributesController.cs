using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing role attributes for ABAC.
/// </summary>
public class RoleAttributesController : Controller
{
    private readonly AccessControlDbContext _context;
    private readonly ILogger<RoleAttributesController> _logger;

    public RoleAttributesController(AccessControlDbContext context, ILogger<RoleAttributesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: RoleAttributes
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.RoleAttributes
            .Where(ra => ra.WorkstreamId == selectedWorkstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ra =>
                ra.AppRoleId.Contains(search) ||
                ra.RoleValue.Contains(search) ||
                (ra.RoleDisplayName != null && ra.RoleDisplayName.Contains(search)));
        }

        var roleAttributes = await query
            .OrderBy(ra => ra.RoleDisplayName ?? ra.RoleValue)
            .ToListAsync();

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

        var roleAttribute = await _context.RoleAttributes
            .FirstOrDefaultAsync(m => m.Id == id);

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
        if (ModelState.IsValid)
        {
            var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

            // Check if role already has attributes for this workstream
            var existing = await _context.RoleAttributes
                .FirstOrDefaultAsync(ra => ra.AppRoleId == model.AppRoleId && ra.RoleValue == model.RoleValue && ra.WorkstreamId == selectedWorkstream);

            if (existing != null)
            {
                ModelState.AddModelError("AppRoleId", "Role attributes already exist for this role in this workstream.");
                return View(model);
            }

            var roleAttribute = new RoleAttribute
            {
                AppRoleId = model.AppRoleId,
                RoleValue = model.RoleValue,
                WorkstreamId = selectedWorkstream,
                RoleDisplayName = model.RoleDisplayName,
                IsActive = model.IsActive,
                AttributesJson = model.AttributesJson
            };

            _context.Add(roleAttribute);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created role attributes for {AppRoleId} ({RoleValue}) in workstream {Workstream}",
                roleAttribute.AppRoleId, roleAttribute.RoleValue, selectedWorkstream);

            return RedirectToAction(nameof(Index));
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

        var roleAttribute = await _context.RoleAttributes.FindAsync(id);
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
            try
            {
                var roleAttribute = await _context.RoleAttributes.FindAsync(id);
                if (roleAttribute == null)
                {
                    return NotFound();
                }

                roleAttribute.AppRoleId = model.AppRoleId;
                roleAttribute.RoleValue = model.RoleValue;
                roleAttribute.WorkstreamId = model.WorkstreamId;
                roleAttribute.RoleDisplayName = model.RoleDisplayName;
                roleAttribute.IsActive = model.IsActive;
                roleAttribute.AttributesJson = model.AttributesJson;

                _context.Update(roleAttribute);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated role attributes for {AppRoleId} ({RoleValue}) in workstream {Workstream}",
                    roleAttribute.AppRoleId, roleAttribute.RoleValue, roleAttribute.WorkstreamId);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await RoleAttributeExists(model.Id))
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

    // GET: RoleAttributes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roleAttribute = await _context.RoleAttributes
            .FirstOrDefaultAsync(m => m.Id == id);

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
        var roleAttribute = await _context.RoleAttributes.FindAsync(id);
        if (roleAttribute != null)
        {
            _context.RoleAttributes.Remove(roleAttribute);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Deleted role attributes for {AppRoleId} ({RoleValue})",
                roleAttribute.AppRoleId, roleAttribute.RoleValue);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> RoleAttributeExists(int id)
    {
        return await _context.RoleAttributes.AnyAsync(e => e.Id == id);
    }
}
