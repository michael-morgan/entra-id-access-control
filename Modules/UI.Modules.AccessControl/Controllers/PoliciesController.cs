using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Services;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing Casbin policies.
/// </summary>
public class PoliciesController : Controller
{
    private readonly AccessControlDbContext _context;
    private readonly GraphGroupService _graphGroupService;
    private readonly GraphUserService _graphUserService;
    private readonly ILogger<PoliciesController> _logger;

    public PoliciesController(
        AccessControlDbContext context,
        GraphGroupService graphGroupService,
        GraphUserService graphUserService,
        ILogger<PoliciesController> logger)
    {
        _context = context;
        _graphGroupService = graphGroupService;
        _graphUserService = graphUserService;
        _logger = logger;
    }

    // GET: Policies
    public async Task<IActionResult> Index(string? policyType = null, string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.CasbinPolicies
            .Where(p => p.WorkstreamId == selectedWorkstream || p.WorkstreamId == null);

        if (!string.IsNullOrWhiteSpace(policyType))
        {
            query = query.Where(p => p.PolicyType == policyType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.V0.Contains(search) ||
                (p.V1 != null && p.V1.Contains(search)) ||
                (p.V2 != null && p.V2.Contains(search)));
        }

        var policies = await query
            .OrderBy(p => p.PolicyType)
            .ThenBy(p => p.V0)
            .ToListAsync();

        // Fetch display names for group/user IDs in V0 (for "g" policies, V0 is often a group GUID)
        var potentialGroupIds = policies
            .Where(p => p.PolicyType == "g" && !string.IsNullOrEmpty(p.V0))
            .Select(p => p.V0)
            .Where(v0 => Guid.TryParse(v0, out _)) // Only GUIDs
            .Distinct()
            .ToList();

        Dictionary<string, string> displayNames = new();

        try
        {
            // Try to fetch as groups first
            var groups = await _graphGroupService.GetGroupsByIdsAsync(potentialGroupIds);
            foreach (var kvp in groups)
            {
                displayNames[kvp.Key] = kvp.Value.DisplayName ?? kvp.Value.MailNickname ?? kvp.Key;
            }

            // For IDs not found as groups, try users
            var notFoundIds = potentialGroupIds.Except(displayNames.Keys).ToList();
            if (notFoundIds.Any())
            {
                var users = await _graphUserService.GetUsersByIdsAsync(notFoundIds);
                foreach (var kvp in users)
                {
                    displayNames[kvp.Key] = kvp.Value.DisplayName ?? kvp.Value.UserPrincipalName ?? kvp.Key;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch display names from Graph API for policy subjects");
        }

        ViewBag.PolicyType = policyType;
        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;
        ViewBag.DisplayNames = displayNames;
        ViewBag.PolicyTypes = await _context.CasbinPolicies
            .Where(p => p.WorkstreamId == selectedWorkstream || p.WorkstreamId == null)
            .Select(p => p.PolicyType)
            .Distinct()
            .OrderBy(pt => pt)
            .ToListAsync();

        return View(policies);
    }

    // GET: Policies/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var policy = await _context.CasbinPolicies
            .FirstOrDefaultAsync(m => m.Id == id);

        if (policy == null)
        {
            return NotFound();
        }

        return View(policy);
    }

    // GET: Policies/Create
    public async Task<IActionResult> Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        // Load available role names for the current workstream
        var roleNames = await _context.CasbinRoles
            .Where(r => r.WorkstreamId == selectedWorkstream && r.IsActive)
            .OrderBy(r => r.RoleName)
            .Select(r => r.RoleName)
            .ToListAsync();

        // Load available resources for the current workstream (including global)
        var resources = await _context.CasbinResources
            .Where(r => r.WorkstreamId == selectedWorkstream || r.WorkstreamId == "*")
            .OrderBy(r => r.ResourcePattern)
            .Select(r => r.ResourcePattern)
            .ToListAsync();

        // Load available workstreams
        var workstreams = await _context.CasbinPolicies
            .Where(p => p.WorkstreamId != null)
            .Select(p => p.WorkstreamId!)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync();

        ViewBag.AvailableRoles = roleNames;
        ViewBag.AvailableResources = resources;
        ViewBag.AvailableWorkstreams = workstreams;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View();
    }

    // POST: Policies/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PolicyViewModel model)
    {
        if (ModelState.IsValid)
        {
            var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

            var policy = new CasbinPolicy
            {
                PolicyType = model.PolicyType,
                V0 = model.V0,
                V1 = model.V1,
                V2 = model.V2,
                V3 = model.V3,
                V4 = model.V4,
                V5 = model.V5,
                WorkstreamId = selectedWorkstream,
                IsActive = model.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Add(policy);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new policy in workstream {Workstream}: {PolicyType} {V0} {V1} {V2}",
                selectedWorkstream, policy.PolicyType, policy.V0, policy.V1, policy.V2);

            TempData["SuccessMessage"] = "Policy created successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Policies/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var policy = await _context.CasbinPolicies.FindAsync(id);
        if (policy == null)
        {
            return NotFound();
        }

        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        // Load available role names for the current workstream
        var roleNames = await _context.CasbinRoles
            .Where(r => r.WorkstreamId == selectedWorkstream && r.IsActive)
            .OrderBy(r => r.RoleName)
            .Select(r => r.RoleName)
            .ToListAsync();

        // Load available resources for the current workstream (including global)
        var resources = await _context.CasbinResources
            .Where(r => r.WorkstreamId == selectedWorkstream || r.WorkstreamId == "*")
            .OrderBy(r => r.ResourcePattern)
            .Select(r => r.ResourcePattern)
            .ToListAsync();

        // Load available workstreams
        var workstreams = await _context.CasbinPolicies
            .Where(p => p.WorkstreamId != null)
            .Select(p => p.WorkstreamId!)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync();

        ViewBag.AvailableRoles = roleNames;
        ViewBag.AvailableResources = resources;
        ViewBag.AvailableWorkstreams = workstreams;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        var model = new PolicyViewModel
        {
            Id = policy.Id,
            PolicyType = policy.PolicyType,
            V0 = policy.V0,
            V1 = policy.V1,
            V2 = policy.V2,
            V3 = policy.V3,
            V4 = policy.V4,
            V5 = policy.V5,
            IsActive = policy.IsActive
        };

        return View(model);
    }

    // POST: Policies/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PolicyViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var policy = await _context.CasbinPolicies.FindAsync(id);
                if (policy == null)
                {
                    return NotFound();
                }

                policy.PolicyType = model.PolicyType;
                policy.V0 = model.V0;
                policy.V1 = model.V1;
                policy.V2 = model.V2;
                policy.V3 = model.V3;
                policy.V4 = model.V4;
                policy.V5 = model.V5;
                policy.IsActive = model.IsActive;
                policy.ModifiedAt = DateTimeOffset.UtcNow;
                policy.ModifiedBy = User.Identity?.Name ?? "System";

                _context.Update(policy);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated policy {Id}: {PolicyType} {V0} {V1} {V2}",
                    policy.Id, policy.PolicyType, policy.V0, policy.V1, policy.V2);

                TempData["SuccessMessage"] = "Policy updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PolicyExists(model.Id))
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

    // GET: Policies/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var policy = await _context.CasbinPolicies
            .FirstOrDefaultAsync(m => m.Id == id);

        if (policy == null)
        {
            return NotFound();
        }

        return View(policy);
    }

    // POST: Policies/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var policy = await _context.CasbinPolicies.FindAsync(id);
        if (policy != null)
        {
            _context.CasbinPolicies.Remove(policy);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Deleted policy {Id}: {PolicyType} {V0} {V1} {V2}",
                policy.Id, policy.PolicyType, policy.V0, policy.V1, policy.V2);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> PolicyExists(int id)
    {
        return await _context.CasbinPolicies.AnyAsync(e => e.Id == id);
    }
}
