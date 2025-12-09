using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Controllers.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

namespace UI.Modules.AccessControl.Controllers.AbacRules;

/// <summary>
/// Controller for managing individual ABAC rules.
/// </summary>
public class AbacRulesController(
    IAbacRuleManagementService ruleManagementService,
    ILogger<AbacRulesController> logger) : Controller
{
    private readonly IAbacRuleManagementService _ruleManagementService = ruleManagementService;
    private readonly ILogger<AbacRulesController> _logger = logger;

    // GET: AbacRules
    public async Task<IActionResult> Index(string? search = null, string? ruleType = null, int? ruleGroupId = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var rules = await _ruleManagementService.GetRulesAsync(selectedWorkstream, search, ruleType, ruleGroupId);

        ViewBag.Search = search;
        ViewBag.RuleType = ruleType;
        ViewBag.RuleGroupId = ruleGroupId;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        // Get available rule groups for filtering
        var ruleGroups = await _ruleManagementService.GetRuleGroupsForWorkstreamAsync(selectedWorkstream);
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

        var rule = await _ruleManagementService.GetRuleByIdAsync(id.Value);
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
        var ruleGroups = await _ruleManagementService.GetRuleGroupsForWorkstreamAsync(selectedWorkstream);
        ViewBag.RuleGroups = new SelectList(ruleGroups, "Id", "GroupName");

        return View();
    }

    // POST: AbacRules/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AbacRuleViewModel model)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        if (ModelState.IsValid)
        {
            var rule = new AbacRule
            {
                WorkstreamId = selectedWorkstream,
                RuleGroupId = model.RuleGroupId,
                RuleName = model.RuleName,
                RuleType = model.RuleType,
                Configuration = model.Configuration,
                IsActive = model.IsActive,
                Priority = model.Priority,
                FailureMessage = model.FailureMessage
            };

            var (success, createdRule, errorMessage) = await _ruleManagementService.CreateRuleAsync(rule, selectedWorkstream, User.Identity?.Name ?? "System");

            if (success)
            {
                _logger.LogInformation("Created ABAC rule {RuleName} ({RuleType}) in workstream {Workstream}",
                    createdRule!.RuleName, createdRule.RuleType, selectedWorkstream);
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, errorMessage!);
        }

        // Reload rule groups if validation fails
        var ruleGroups = await _ruleManagementService.GetRuleGroupsForWorkstreamAsync(selectedWorkstream);
        ViewBag.RuleGroups = new SelectList(ruleGroups, "Id", "GroupName");
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(model);
    }

    // GET: AbacRules/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rule = await _ruleManagementService.GetRuleByIdAsync(id.Value);
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
        var ruleGroups = await _ruleManagementService.GetRuleGroupsForWorkstreamAsync(rule.WorkstreamId);
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
            var rule = new AbacRule
            {
                Id = model.Id,
                WorkstreamId = model.WorkstreamId,
                RuleGroupId = model.RuleGroupId,
                RuleName = model.RuleName,
                RuleType = model.RuleType,
                Configuration = model.Configuration,
                IsActive = model.IsActive,
                Priority = model.Priority,
                FailureMessage = model.FailureMessage
            };

            var (success, errorMessage) = await _ruleManagementService.UpdateRuleAsync(id, rule, User.Identity?.Name ?? "System");

            if (success)
            {
                _logger.LogInformation("Updated ABAC rule {RuleName} in workstream {Workstream}",
                    model.RuleName, model.WorkstreamId);
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, errorMessage!);
        }

        // Reload rule groups if validation fails
        var ruleGroups = await _ruleManagementService.GetRuleGroupsForWorkstreamAsync(model.WorkstreamId);
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

        var rule = await _ruleManagementService.GetRuleByIdAsync(id.Value);
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
        var rule = await _ruleManagementService.GetRuleByIdAsync(id);
        if (rule != null)
        {
            var success = await _ruleManagementService.DeleteRuleAsync(id);
            if (success)
            {
                _logger.LogWarning("Deleted ABAC rule {RuleName} ({RuleType})", rule.RuleName, rule.RuleType);
            }
        }

        return RedirectToAction(nameof(Index));
    }
}
