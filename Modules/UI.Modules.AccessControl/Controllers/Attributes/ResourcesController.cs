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

namespace UI.Modules.AccessControl.Controllers.Attributes;

/// <summary>
/// Controller for managing Casbin resource definitions.
/// </summary>
public class ResourcesController(
    IResourceManagementService resourceManagementService,
    ILogger<ResourcesController> logger) : Controller
{
    private readonly IResourceManagementService _resourceManagementService = resourceManagementService;
    private readonly ILogger<ResourcesController> _logger = logger;

    // GET: Resources
    public async Task<IActionResult> Index(string? workstreamFilter = null, string? search = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var resources = await _resourceManagementService.GetResourcesAsync(
            selectedWorkstream, workstreamFilter, search);

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

        var resource = await _resourceManagementService.GetResourceByIdAsync(id.Value);
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
            var createdBy = User.Identity?.Name ?? "System";
            var (success, resource, errorMessage) =
                await _resourceManagementService.CreateResourceAsync(model, createdBy);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage!);
                ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
                return View(model);
            }

            _logger.LogInformation("Created new resource in workstream {Workstream}: {ResourcePattern} ({DisplayName})",
                resource!.WorkstreamId, resource.ResourcePattern, resource.DisplayName);

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

        var resource = await _resourceManagementService.GetResourceByIdAsync(id.Value);
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
            var modifiedBy = User.Identity?.Name ?? "System";
            var (success, errorMessage) =
                await _resourceManagementService.UpdateResourceAsync(id, model, modifiedBy);

            if (!success)
            {
                if (errorMessage == "Resource not found")
                {
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, errorMessage!);
                ViewBag.SelectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
                return View(model);
            }

            _logger.LogInformation("Updated resource {Id}: {ResourcePattern} ({DisplayName})",
                id, model.ResourcePattern, model.DisplayName);

            TempData["SuccessMessage"] = "Resource updated successfully.";
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

        var resource = await _resourceManagementService.GetResourceByIdAsync(id.Value);
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
        var success = await _resourceManagementService.DeleteResourceAsync(id);

        if (success)
        {
            _logger.LogWarning("Deleted resource {Id}", id);
            TempData["SuccessMessage"] = "Resource deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
