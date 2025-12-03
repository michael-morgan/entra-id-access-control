using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing Casbin resource definitions.
/// </summary>
public class ResourcesController : Controller
{
    private readonly AccessControlDbContext _context;
    private readonly ILogger<ResourcesController> _logger;

    public ResourcesController(AccessControlDbContext context, ILogger<ResourcesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Resources
    public async Task<IActionResult> Index(string? workstreamFilter = null, string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.CasbinResources
            .Where(r => r.WorkstreamId == selectedWorkstream || r.WorkstreamId == "*");

        if (!string.IsNullOrWhiteSpace(workstreamFilter))
        {
            query = query.Where(r => r.WorkstreamId == workstreamFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.ResourcePattern.Contains(search) ||
                r.DisplayName.Contains(search) ||
                (r.Description != null && r.Description.Contains(search)));
        }

        var resources = await query
            .OrderBy(r => r.WorkstreamId)
            .ThenBy(r => r.ResourcePattern)
            .ToListAsync();

        ViewBag.WorkstreamFilter = workstreamFilter;
        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(resources);
    }

    // GET: Resources/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var resource = await _context.CasbinResources
            .FirstOrDefaultAsync(m => m.Id == id);

        if (resource == null)
        {
            return NotFound();
        }

        ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        return View(resource);
    }

    // GET: Resources/Create
    public IActionResult Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;

        var model = new ResourceViewModel
        {
            WorkstreamId = selectedWorkstream
        };

        return View(model);
    }

    // POST: Resources/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResourceViewModel model)
    {
        if (ModelState.IsValid)
        {
            var resource = new CasbinResource
            {
                ResourcePattern = model.ResourcePattern,
                WorkstreamId = model.WorkstreamId,
                DisplayName = model.DisplayName,
                Description = model.Description,
                ParentResource = model.ParentResource,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Add(resource);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new resource in workstream {Workstream}: {ResourcePattern} ({DisplayName})",
                resource.WorkstreamId, resource.ResourcePattern, resource.DisplayName);

            TempData["SuccessMessage"] = "Resource created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        return View(model);
    }

    // GET: Resources/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var resource = await _context.CasbinResources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }

        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;

        var model = new ResourceViewModel
        {
            Id = resource.Id,
            ResourcePattern = resource.ResourcePattern,
            WorkstreamId = resource.WorkstreamId,
            DisplayName = resource.DisplayName,
            Description = resource.Description,
            ParentResource = resource.ParentResource,
            CreatedAt = resource.CreatedAt,
            CreatedBy = resource.CreatedBy,
            ModifiedAt = resource.ModifiedAt,
            ModifiedBy = resource.ModifiedBy
        };

        return View(model);
    }

    // POST: Resources/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ResourceViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var resource = await _context.CasbinResources.FindAsync(id);
                if (resource == null)
                {
                    return NotFound();
                }

                resource.ResourcePattern = model.ResourcePattern;
                resource.WorkstreamId = model.WorkstreamId;
                resource.DisplayName = model.DisplayName;
                resource.Description = model.Description;
                resource.ParentResource = model.ParentResource;
                resource.ModifiedAt = DateTimeOffset.UtcNow;
                resource.ModifiedBy = User.Identity?.Name ?? "System";

                _context.Update(resource);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated resource {Id}: {ResourcePattern} ({DisplayName})",
                    resource.Id, resource.ResourcePattern, resource.DisplayName);

                TempData["SuccessMessage"] = "Resource updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ResourceExists(model.Id))
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

        ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        return View(model);
    }

    // GET: Resources/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var resource = await _context.CasbinResources
            .FirstOrDefaultAsync(m => m.Id == id);

        if (resource == null)
        {
            return NotFound();
        }

        ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        return View(resource);
    }

    // POST: Resources/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var resource = await _context.CasbinResources.FindAsync(id);
        if (resource != null)
        {
            _context.CasbinResources.Remove(resource);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Deleted resource {Id}: {ResourcePattern} ({DisplayName})",
                resource.Id, resource.ResourcePattern, resource.DisplayName);

            TempData["SuccessMessage"] = "Resource deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ResourceExists(int id)
    {
        return await _context.CasbinResources.AnyAsync(e => e.Id == id);
    }
}
