using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

public class AbacRuleGroupsController : Controller
{
    private readonly AccessControlDbContext _context;
    private readonly ILogger<AbacRuleGroupsController> _logger;

    public AbacRuleGroupsController(AccessControlDbContext context, ILogger<AbacRuleGroupsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: AbacRuleGroups
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.AbacRuleGroups
            .Include(g => g.ParentGroup)
            .Include(g => g.ChildGroups)
            .Include(g => g.Rules)
            .Where(g => g.WorkstreamId == selectedWorkstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g => g.GroupName.Contains(search) ||
                                   (g.Description != null && g.Description.Contains(search)));
        }

        var groups = await query
            .OrderBy(g => g.Priority)
            .ThenBy(g => g.GroupName)
            .ToListAsync();

        var viewModels = groups.Select(g => new AbacRuleGroupViewModel
        {
            Id = g.Id,
            WorkstreamId = g.WorkstreamId,
            GroupName = g.GroupName,
            Description = g.Description,
            ParentGroupId = g.ParentGroupId,
            ParentGroupName = g.ParentGroup?.GroupName,
            LogicalOperator = g.LogicalOperator,
            Resource = g.Resource,
            Action = g.Action,
            IsActive = g.IsActive,
            Priority = g.Priority,
            ChildGroupCount = g.ChildGroups.Count,
            RuleCount = g.Rules.Count
        }).ToList();

        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(viewModels);
    }

    // GET: AbacRuleGroups/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var group = await _context.AbacRuleGroups
            .Include(g => g.ParentGroup)
            .Include(g => g.ChildGroups)
            .Include(g => g.Rules)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (group == null) return NotFound();

        return View(group);
    }

    // GET: AbacRuleGroups/Create
    public async Task<IActionResult> Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        await PopulateParentGroupsDropdown(selectedWorkstream);
        await PopulateResourcesDropdown(selectedWorkstream);
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
            var group = new AbacRuleGroup
            {
                WorkstreamId = selectedWorkstream,
                GroupName = model.GroupName,
                Description = model.Description,
                ParentGroupId = model.ParentGroupId,
                LogicalOperator = model.LogicalOperator,
                Resource = model.Resource,
                Action = model.Action,
                IsActive = model.IsActive,
                Priority = model.Priority,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Add(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created rule group {GroupName} for workstream {Workstream}",
                model.GroupName, selectedWorkstream);

            return RedirectToAction(nameof(Index));
        }

        await PopulateParentGroupsDropdown(selectedWorkstream, model.ParentGroupId);
        await PopulateResourcesDropdown(selectedWorkstream);
        return View(model);
    }

    // GET: AbacRuleGroups/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var group = await _context.AbacRuleGroups.FindAsync(id);
        if (group == null) return NotFound();

        var model = new AbacRuleGroupViewModel
        {
            Id = group.Id,
            WorkstreamId = group.WorkstreamId,
            GroupName = group.GroupName,
            Description = group.Description,
            ParentGroupId = group.ParentGroupId,
            LogicalOperator = group.LogicalOperator,
            Resource = group.Resource,
            Action = group.Action,
            IsActive = group.IsActive,
            Priority = group.Priority
        };

        await PopulateParentGroupsDropdown(group.WorkstreamId, group.ParentGroupId, group.Id);
        await PopulateResourcesDropdown(group.WorkstreamId);
        return View(model);
    }

    // POST: AbacRuleGroups/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AbacRuleGroupViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var group = await _context.AbacRuleGroups.FindAsync(id);
                if (group == null) return NotFound();

                group.GroupName = model.GroupName;
                group.Description = model.Description;
                group.ParentGroupId = model.ParentGroupId;
                group.LogicalOperator = model.LogicalOperator;
                group.Resource = model.Resource;
                group.Action = model.Action;
                group.IsActive = model.IsActive;
                group.Priority = model.Priority;
                group.ModifiedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated rule group {GroupName}", model.GroupName);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AbacRuleGroupExists(model.Id))
                    return NotFound();
                throw;
            }
        }

        await PopulateParentGroupsDropdown(model.WorkstreamId, model.ParentGroupId, model.Id);
        await PopulateResourcesDropdown(model.WorkstreamId);
        return View(model);
    }

    // GET: AbacRuleGroups/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var group = await _context.AbacRuleGroups
            .Include(g => g.ParentGroup)
            .Include(g => g.ChildGroups)
            .Include(g => g.Rules)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (group == null) return NotFound();

        return View(group);
    }

    // POST: AbacRuleGroups/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var group = await _context.AbacRuleGroups
            .Include(g => g.ChildGroups)
            .Include(g => g.Rules)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group != null)
        {
            // Check for child groups or rules
            if (group.ChildGroups.Any() || group.Rules.Any())
            {
                ModelState.AddModelError("", "Cannot delete group with child groups or rules. Remove them first.");
                return View(group);
            }

            _context.AbacRuleGroups.Remove(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted rule group {GroupName}", group.GroupName);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> AbacRuleGroupExists(int id)
    {
        return await _context.AbacRuleGroups.AnyAsync(e => e.Id == id);
    }

    private async Task PopulateParentGroupsDropdown(string workstreamId, int? selectedValue = null, int? excludeId = null)
    {
        var groups = await _context.AbacRuleGroups
            .Where(g => g.WorkstreamId == workstreamId && g.Id != excludeId)
            .OrderBy(g => g.GroupName)
            .Select(g => new { g.Id, g.GroupName })
            .ToListAsync();

        ViewBag.ParentGroups = new SelectList(groups, "Id", "GroupName", selectedValue);
    }

    private async Task PopulateResourcesDropdown(string workstreamId)
    {
        var resources = await _context.CasbinResources
            .Where(r => r.WorkstreamId == workstreamId || r.WorkstreamId == "*")
            .OrderBy(r => r.ResourcePattern)
            .Select(r => r.ResourcePattern)
            .ToListAsync();

        ViewBag.AvailableResources = resources;
    }
}
