using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Controllers.Home;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
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

namespace UI.Modules.AccessControl.Controllers.Authorization;

/// <summary>
/// Controller for managing Attribute Schemas.
/// </summary>
public class AttributeSchemasController(
    IAttributeSchemaManagementService schemaManagementService,
    ILogger<AttributeSchemasController> logger) : Controller
{
    private readonly IAttributeSchemaManagementService _schemaManagementService = schemaManagementService;
    private readonly ILogger<AttributeSchemasController> _logger = logger;

    // GET: AttributeSchemas
    public async Task<IActionResult> Index(string? search = null, string? attributeLevel = null)
    {
        var selectedWorkstream = WorkstreamController.GetSelectedWorkstream(HttpContext);
        var schemas = await _schemaManagementService.GetSchemasAsync(selectedWorkstream, search, attributeLevel);

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

        var schema = await _schemaManagementService.GetSchemaByIdAsync(id.Value);
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
            var schema = MapToEntity(viewModel, selectedWorkstream);
            var (success, createdSchema, errorMessage) = await _schemaManagementService.CreateSchemaAsync(
                schema, selectedWorkstream, User.Identity?.Name ?? "System");

            if (success)
            {
                TempData["SuccessMessage"] = $"Attribute schema '{createdSchema!.AttributeDisplayName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogError("Error creating attribute schema: {Error}", errorMessage);
            ModelState.AddModelError("", errorMessage!);
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

        var schema = await _schemaManagementService.GetSchemaByIdAsync(id.Value);
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
            var schema = await _schemaManagementService.GetSchemaByIdAsync(id);
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

            var (success, errorMessage) = await _schemaManagementService.UpdateSchemaAsync(
                id, schema, User.Identity?.Name ?? "System");

            if (success)
            {
                TempData["SuccessMessage"] = $"Attribute schema '{schema.AttributeDisplayName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, errorMessage!);
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

        var schema = await _schemaManagementService.GetSchemaByIdAsync(id.Value);
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
        var schema = await _schemaManagementService.GetSchemaByIdAsync(id);
        if (schema != null)
        {
            var success = await _schemaManagementService.DeleteSchemaAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = $"Attribute schema '{schema.AttributeDisplayName}' deleted successfully.";
            }
        }

        return RedirectToAction(nameof(Index));
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
