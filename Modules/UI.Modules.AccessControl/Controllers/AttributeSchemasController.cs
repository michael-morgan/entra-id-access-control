using Api.Modules.AccessControl.Persistence;
using UI.Modules.AccessControl.Models;

using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace UI.Modules.AccessControl.Controllers;

/// <summary>
/// Controller for managing Attribute Schemas.
/// </summary>
public class AttributeSchemasController(AccessControlDbContext context, ILogger<AttributeSchemasController> logger) : Controller
{
    private readonly AccessControlDbContext _context = context;
    private readonly ILogger<AttributeSchemasController> _logger = logger;

    // GET: AttributeSchemas
    public async Task<IActionResult> Index(string? search = null, string? attributeLevel = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var query = _context.AttributeSchemas
            .Where(a => a.WorkstreamId == selectedWorkstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.AttributeName.Contains(search) || a.AttributeDisplayName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(attributeLevel))
        {
            query = query.Where(a => a.AttributeLevel == attributeLevel);
        }

        var schemas = await query
            .OrderBy(a => a.AttributeLevel)
            .ThenBy(a => a.DisplayOrder)
            .ThenBy(a => a.AttributeName)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.AttributeLevel = attributeLevel;
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View(schemas);
    }

    // GET: AttributeSchemas/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var schema = await _context.AttributeSchemas
            .FirstOrDefaultAsync(m => m.Id == id);

        if (schema == null)
        {
            return NotFound();
        }

        var viewModel = MapToViewModel(schema);
        ViewBag.SelectedWorkstream = schema.WorkstreamId;

        return View(viewModel);
    }

    // GET: AttributeSchemas/Create
    public IActionResult Create()
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        ViewBag.SelectedWorkstream = selectedWorkstream;

        return View();
    }

    // POST: AttributeSchemas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AttributeSchemaViewModel viewModel)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        if (ModelState.IsValid)
        {
            try
            {
                var schema = MapToEntity(viewModel, selectedWorkstream);
                schema.CreatedAt = DateTimeOffset.UtcNow;
                schema.CreatedBy = User.Identity?.Name ?? "System";

                _context.Add(schema);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Attribute schema '{schema.AttributeDisplayName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attribute schema");
                ModelState.AddModelError("", $"Error creating schema: {ex.Message}");
            }
        }
        else
        {
            _logger.LogWarning("ModelState is invalid. Errors: {Errors}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View(viewModel);
    }

    // GET: AttributeSchemas/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var schema = await _context.AttributeSchemas.FindAsync(id);
        if (schema == null)
        {
            return NotFound();
        }

        var viewModel = MapToViewModel(schema);
        ViewBag.SelectedWorkstream = schema.WorkstreamId;

        return View(viewModel);
    }

    // POST: AttributeSchemas/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AttributeSchemaViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);

        if (ModelState.IsValid)
        {
            try
            {
                var schema = await _context.AttributeSchemas.FindAsync(id);
                if (schema == null)
                {
                    return NotFound();
                }

                // Update properties
                schema.AttributeLevel = viewModel.AttributeLevel;
                schema.AttributeName = viewModel.AttributeName;
                schema.AttributeDisplayName = viewModel.AttributeDisplayName;
                schema.DataType = viewModel.DataType;
                schema.IsRequired = viewModel.IsRequired;
                schema.DefaultValue = viewModel.DefaultValue;
                schema.ValidationRules = BuildValidationRulesJson(viewModel);
                schema.Description = viewModel.Description;
                schema.DisplayOrder = viewModel.DisplayOrder;
                schema.IsActive = viewModel.IsActive;
                schema.ModifiedAt = DateTimeOffset.UtcNow;
                schema.ModifiedBy = User.Identity?.Name ?? "System";

                _context.Update(schema);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Attribute schema '{schema.AttributeDisplayName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttributeSchemaExists(viewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        ViewBag.SelectedWorkstream = selectedWorkstream;
        return View(viewModel);
    }

    // GET: AttributeSchemas/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var schema = await _context.AttributeSchemas
            .FirstOrDefaultAsync(m => m.Id == id);

        if (schema == null)
        {
            return NotFound();
        }

        var viewModel = MapToViewModel(schema);
        ViewBag.SelectedWorkstream = schema.WorkstreamId;

        return View(viewModel);
    }

    // POST: AttributeSchemas/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var schema = await _context.AttributeSchemas.FindAsync(id);
        if (schema != null)
        {
            _context.AttributeSchemas.Remove(schema);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Attribute schema '{schema.AttributeDisplayName}' deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool AttributeSchemaExists(int id)
    {
        return _context.AttributeSchemas.Any(e => e.Id == id);
    }

    private AttributeSchemaViewModel MapToViewModel(AttributeSchema schema)
    {
        var viewModel = new AttributeSchemaViewModel
        {
            Id = schema.Id,
            WorkstreamId = schema.WorkstreamId,
            AttributeLevel = schema.AttributeLevel,
            AttributeName = schema.AttributeName,
            AttributeDisplayName = schema.AttributeDisplayName,
            DataType = schema.DataType,
            IsRequired = schema.IsRequired,
            DefaultValue = schema.DefaultValue,
            ValidationRules = schema.ValidationRules,
            Description = schema.Description,
            DisplayOrder = schema.DisplayOrder,
            IsActive = schema.IsActive,
            CreatedAt = schema.CreatedAt,
            CreatedBy = schema.CreatedBy,
            ModifiedAt = schema.ModifiedAt,
            ModifiedBy = schema.ModifiedBy
        };

        // Extract allowed values from ValidationRules JSON
        if (!string.IsNullOrWhiteSpace(schema.ValidationRules))
        {
            try
            {
                var validationRules = JsonDocument.Parse(schema.ValidationRules);
                if (validationRules.RootElement.TryGetProperty("allowedValues", out var allowedValuesElement))
                {
                    var allowedValues = new List<string>();
                    foreach (var item in allowedValuesElement.EnumerateArray())
                    {
                        allowedValues.Add(item.ToString());
                    }
                    viewModel.AllowedValuesInput = string.Join(", ", allowedValues);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse ValidationRules JSON for schema {SchemaId}", schema.Id);
            }
        }

        return viewModel;
    }

    private AttributeSchema MapToEntity(AttributeSchemaViewModel viewModel, string workstreamId)
    {
        return new AttributeSchema
        {
            WorkstreamId = workstreamId,
            AttributeLevel = viewModel.AttributeLevel,
            AttributeName = viewModel.AttributeName,
            AttributeDisplayName = viewModel.AttributeDisplayName,
            DataType = viewModel.DataType,
            IsRequired = viewModel.IsRequired,
            DefaultValue = viewModel.DefaultValue,
            ValidationRules = BuildValidationRulesJson(viewModel),
            Description = viewModel.Description,
            DisplayOrder = viewModel.DisplayOrder,
            IsActive = viewModel.IsActive
        };
    }

    private static string? BuildValidationRulesJson(AttributeSchemaViewModel viewModel)
    {
        if (string.IsNullOrWhiteSpace(viewModel.AllowedValuesInput))
        {
            return viewModel.ValidationRules;
        }

        // Parse allowed values from comma-separated input
        var allowedValues = viewModel.AllowedValuesInput
            .Split(',')
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (allowedValues.Count == 0)
        {
            return viewModel.ValidationRules;
        }

        // Create JSON with allowedValues
        var validationRules = new
        {
            allowedValues
        };

        return JsonSerializer.Serialize(validationRules);
    }
}
