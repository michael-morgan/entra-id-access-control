using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing group attributes for ABAC.
/// </summary>
public class GroupAttributesController : Controller
{
    private readonly AccessControlDbContext _context;
    private readonly ILogger<GroupAttributesController> _logger;

    public GroupAttributesController(AccessControlDbContext context, ILogger<GroupAttributesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: GroupAttributes
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.GroupAttributes
            .Where(ga => ga.WorkstreamId == selectedWorkstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ga =>
                ga.GroupId.Contains(search) ||
                (ga.GroupName != null && ga.GroupName.Contains(search)));
        }

        var groupAttributes = await query
            .OrderBy(ga => ga.GroupName ?? ga.GroupId)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(groupAttributes);
    }

    // GET: GroupAttributes/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var groupAttribute = await _context.GroupAttributes
            .FirstOrDefaultAsync(m => m.Id == id);

        if (groupAttribute == null)
        {
            return NotFound();
        }

        return View(groupAttribute);
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

            // Check if group already has attributes for this workstream
            var existing = await _context.GroupAttributes
                .FirstOrDefaultAsync(ga => ga.GroupId == model.GroupId && ga.WorkstreamId == selectedWorkstream);

            if (existing != null)
            {
                ModelState.AddModelError("GroupId", "Group attributes already exist for this group in this workstream.");
                return View(model);
            }

            var groupAttribute = new GroupAttribute
            {
                GroupId = model.GroupId,
                WorkstreamId = selectedWorkstream,
                GroupName = model.GroupName,
                IsActive = model.IsActive,
                AttributesJson = model.AttributesJson
            };

            _context.Add(groupAttribute);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created group attributes for {GroupId} in workstream {Workstream}",
                groupAttribute.GroupId, selectedWorkstream);

            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: GroupAttributes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var groupAttribute = await _context.GroupAttributes.FindAsync(id);
        if (groupAttribute == null)
        {
            return NotFound();
        }

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
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var groupAttribute = await _context.GroupAttributes.FindAsync(id);
                if (groupAttribute == null)
                {
                    return NotFound();
                }

                groupAttribute.GroupId = model.GroupId;
                groupAttribute.WorkstreamId = model.WorkstreamId;
                groupAttribute.GroupName = model.GroupName;
                groupAttribute.IsActive = model.IsActive;
                groupAttribute.AttributesJson = model.AttributesJson;

                _context.Update(groupAttribute);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated group attributes for {GroupId} in workstream {Workstream}",
                    groupAttribute.GroupId, groupAttribute.WorkstreamId);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await GroupAttributeExists(model.Id))
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

    // GET: GroupAttributes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var groupAttribute = await _context.GroupAttributes
            .FirstOrDefaultAsync(m => m.Id == id);

        if (groupAttribute == null)
        {
            return NotFound();
        }

        return View(groupAttribute);
    }

    // POST: GroupAttributes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var groupAttribute = await _context.GroupAttributes.FindAsync(id);
        if (groupAttribute != null)
        {
            _context.GroupAttributes.Remove(groupAttribute);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Deleted group attributes for {GroupId}", groupAttribute.GroupId);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> GroupAttributeExists(int id)
    {
        return await _context.GroupAttributes.AnyAsync(e => e.Id == id);
    }
}
