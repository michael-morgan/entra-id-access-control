using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing individual ABAC rules.
/// </summary>
public class AbacRulesController(AccessControlDbContext context, ILogger<AbacRulesController> logger) : Controller
{
    private readonly AccessControlDbContext _context = context;
    private readonly ILogger<AbacRulesController> _logger = logger;

    // GET: AbacRules
    public async Task<IActionResult> Index(string? search = null, string? ruleType = null, int? ruleGroupId = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.AbacRules
            .Include(r => r.RuleGroup)
            .Where(r => r.WorkstreamId == selectedWorkstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.RuleName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(ruleType))
        {
            query = query.Where(r => r.RuleType == ruleType);
        }

        if (ruleGroupId.HasValue)
        {
            query = query.Where(r => r.RuleGroupId == ruleGroupId.Value);
        }

        var rules = await query
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.RuleName)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.RuleType = ruleType;
        ViewBag.RuleGroupId = ruleGroupId;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        // Get available rule groups for filtering
        var ruleGroups = await _context.AbacRuleGroups
            .Where(rg => rg.WorkstreamId == selectedWorkstream)
            .OrderBy(rg => rg.GroupName)
            .ToListAsync();
        ViewBag.RuleGroups = new SelectList(ruleGroups, "Id", "GroupName");

        return View(rules);
    }

    // GET: AbacRules/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rule = await _context.AbacRules
            .Include(r => r.RuleGroup)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (rule == null)
        {
            return NotFound();
        }

        return View(rule);
    }

    // GET: AbacRules/Create
    public async Task<IActionResult> Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;

        // Load available rule groups
        var ruleGroups = await _context.AbacRuleGroups
            .Where(rg => rg.WorkstreamId == selectedWorkstream)
            .OrderBy(rg => rg.GroupName)
            .ToListAsync();
        ViewBag.RuleGroups = new SelectList(ruleGroups, "Id", "GroupName");

        return View();
    }

    // POST: AbacRules/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AbacRuleViewModel model)
    {
        if (ModelState.IsValid)
        {
            var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

            var rule = new AbacRule
            {
                WorkstreamId = selectedWorkstream,
                RuleGroupId = model.RuleGroupId,
                RuleName = model.RuleName,
                RuleType = model.RuleType,
                Configuration = model.Configuration,
                IsActive = model.IsActive,
                Priority = model.Priority,
                FailureMessage = model.FailureMessage,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            _context.Add(rule);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created ABAC rule {RuleName} ({RuleType}) in workstream {Workstream}",
                rule.RuleName, rule.RuleType, selectedWorkstream);

            return RedirectToAction(nameof(Index));
        }

        // Reload rule groups if validation fails
        var selectedWorkstreamReload = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var ruleGroups = await _context.AbacRuleGroups
            .Where(rg => rg.WorkstreamId == selectedWorkstreamReload)
            .OrderBy(rg => rg.GroupName)
            .ToListAsync();
        ViewBag.RuleGroups = new SelectList(ruleGroups, "Id", "GroupName");
        ViewBag.SelectedWorkstream = selectedWorkstreamReload;

        return View(model);
    }

    // GET: AbacRules/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rule = await _context.AbacRules
            .Include(r => r.RuleGroup)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            return NotFound();
        }

        var model = new AbacRuleViewModel
        {
            Id = rule.Id,
            WorkstreamId = rule.WorkstreamId,
            RuleGroupId = rule.RuleGroupId,
            RuleGroupName = rule.RuleGroup?.GroupName,
            RuleName = rule.RuleName,
            RuleType = rule.RuleType,
            Configuration = rule.Configuration,
            IsActive = rule.IsActive,
            Priority = rule.Priority,
            FailureMessage = rule.FailureMessage,
            CreatedAt = rule.CreatedAt,
            ModifiedAt = rule.ModifiedAt,
            CreatedBy = rule.CreatedBy,
            ModifiedBy = rule.ModifiedBy
        };

        ViewBag.SelectedWorkstream = rule.WorkstreamId;

        // Load available rule groups
        var ruleGroups = await _context.AbacRuleGroups
            .Where(rg => rg.WorkstreamId == rule.WorkstreamId)
            .OrderBy(rg => rg.GroupName)
            .ToListAsync();
        ViewBag.RuleGroups = new SelectList(ruleGroups, "Id", "GroupName", rule.RuleGroupId);

        return View(model);
    }

    // POST: AbacRules/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AbacRuleViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var rule = await _context.AbacRules.FindAsync(id);
                if (rule == null)
                {
                    return NotFound();
                }

                rule.RuleGroupId = model.RuleGroupId;
                rule.RuleName = model.RuleName;
                rule.RuleType = model.RuleType;
                rule.Configuration = model.Configuration;
                rule.IsActive = model.IsActive;
                rule.Priority = model.Priority;
                rule.FailureMessage = model.FailureMessage;
                rule.ModifiedAt = DateTimeOffset.UtcNow;
                rule.ModifiedBy = User.Identity?.Name;

                _context.Update(rule);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated ABAC rule {RuleName} ({RuleType}) in workstream {Workstream}",
                    rule.RuleName, rule.RuleType, rule.WorkstreamId);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await RuleExists(model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // Reload rule groups if validation fails
        var ruleGroups = await _context.AbacRuleGroups
            .Where(rg => rg.WorkstreamId == model.WorkstreamId)
            .OrderBy(rg => rg.GroupName)
            .ToListAsync();
        ViewBag.RuleGroups = new SelectList(ruleGroups, "Id", "GroupName", model.RuleGroupId);
        ViewBag.SelectedWorkstream = model.WorkstreamId;

        return View(model);
    }

    // GET: AbacRules/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rule = await _context.AbacRules
            .Include(r => r.RuleGroup)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (rule == null)
        {
            return NotFound();
        }

        return View(rule);
    }

    // POST: AbacRules/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var rule = await _context.AbacRules.FindAsync(id);
        if (rule != null)
        {
            _context.AbacRules.Remove(rule);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Deleted ABAC rule {RuleName} ({RuleType})", rule.RuleName, rule.RuleType);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> RuleExists(int id)
    {
        return await _context.AbacRules.AnyAsync(e => e.Id == id);
    }
}
