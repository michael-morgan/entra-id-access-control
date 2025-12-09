using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Services;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing user attributes for ABAC.
/// </summary>
public class UserAttributesController(
    AccessControlDbContext context,
    GraphUserService graphUserService,
    ILogger<UserAttributesController> logger) : Controller
{
    private readonly AccessControlDbContext _context = context;
    private readonly GraphUserService _graphUserService = graphUserService;
    private readonly ILogger<UserAttributesController> _logger = logger;

    // GET: UserAttributes
    public async Task<IActionResult> Index(string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.UserAttributes
            .Where(ua => ua.WorkstreamId == selectedWorkstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ua => ua.UserId.Contains(search));
        }

        var userAttributes = await query
            .OrderBy(ua => ua.UserId)
            .ToListAsync();

        // Fetch user display names from Entra ID
        var userIds = userAttributes.Select(ua => ua.UserId).Distinct().ToList();
        Dictionary<string, string> userDisplayNames = [];

        try
        {
            var users = await _graphUserService.GetUsersByIdsAsync(userIds);
            userDisplayNames = users.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.DisplayName ?? kvp.Value.UserPrincipalName ?? kvp.Key
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch user display names from Graph API. Showing IDs only.");
        }

        ViewBag.Search = search;
        ViewBag.SelectedWorkstream = selectedWorkstream;
        ViewBag.UserDisplayNames = userDisplayNames;

        return View(userAttributes);
    }

    // GET: UserAttributes/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userAttribute = await _context.UserAttributes
            .FirstOrDefaultAsync(m => m.Id == id);

        if (userAttribute == null)
        {
            return NotFound();
        }

        // Fetch user display name from Entra ID
        try
        {
            var user = await _graphUserService.GetUserByIdAsync(userAttribute.UserId);
            ViewBag.UserDisplayName = user?.DisplayName ?? user?.UserPrincipalName ?? userAttribute.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch user display name for {UserId}", userAttribute.UserId);
            ViewBag.UserDisplayName = userAttribute.UserId;
        }

        return View(userAttribute);
    }

    // GET: UserAttributes/Create
    public IActionResult Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View();
    }

    // POST: UserAttributes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserAttributeViewModel model)
    {
        if (ModelState.IsValid)
        {
            var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

            // Check if user already has attributes for this workstream
            var existing = await _context.UserAttributes
                .FirstOrDefaultAsync(ua => ua.UserId == model.UserId && ua.WorkstreamId == selectedWorkstream);

            if (existing != null)
            {
                ModelState.AddModelError("UserId", "User attributes already exist for this user in this workstream.");
                return View(model);
            }

            var userAttribute = new UserAttribute
            {
                UserId = model.UserId,
                WorkstreamId = selectedWorkstream,
                AttributesJson = model.AttributesJson
            };

            _context.Add(userAttribute);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created user attributes for {UserId} in workstream {Workstream}",
                userAttribute.UserId, selectedWorkstream);

            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: UserAttributes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userAttribute = await _context.UserAttributes.FindAsync(id);
        if (userAttribute == null)
        {
            return NotFound();
        }

        var model = new UserAttributeViewModel
        {
            Id = userAttribute.Id,
            UserId = userAttribute.UserId,
            WorkstreamId = userAttribute.WorkstreamId,
            AttributesJson = userAttribute.AttributesJson
        };

        ViewBag.SelectedWorkstream = userAttribute.WorkstreamId;

        return View(model);
    }

    // POST: UserAttributes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserAttributeViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var userAttribute = await _context.UserAttributes.FindAsync(id);
                if (userAttribute == null)
                {
                    return NotFound();
                }

                userAttribute.UserId = model.UserId;
                userAttribute.WorkstreamId = model.WorkstreamId;
                userAttribute.AttributesJson = model.AttributesJson;

                _context.Update(userAttribute);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user attributes for {UserId} in workstream {Workstream}",
                    userAttribute.UserId, userAttribute.WorkstreamId);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserAttributeExists(model.Id))
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

    // GET: UserAttributes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userAttribute = await _context.UserAttributes
            .FirstOrDefaultAsync(m => m.Id == id);

        if (userAttribute == null)
        {
            return NotFound();
        }

        return View(userAttribute);
    }

    // POST: UserAttributes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userAttribute = await _context.UserAttributes.FindAsync(id);
        if (userAttribute != null)
        {
            _context.UserAttributes.Remove(userAttribute);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Deleted user attributes for {UserId}", userAttribute.UserId);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> UserAttributeExists(int id)
    {
        return await _context.UserAttributes.AnyAsync(e => e.Id == id);
    }
}
