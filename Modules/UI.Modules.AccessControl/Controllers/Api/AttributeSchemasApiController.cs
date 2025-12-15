using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UI.Modules.AccessControl.Services;
using UI.Modules.AccessControl.Services.Attributes;

namespace UI.Modules.AccessControl.Controllers.Api;

[ApiController]
[Route("api/attribute-schemas")]
public class AttributeSchemasApiController(
    IAttributeSchemaManagementService schemaManagementService,
    ILogger<AttributeSchemasApiController> logger) : ControllerBase
{
    private readonly IAttributeSchemaManagementService _schemaManagementService = schemaManagementService;
    private readonly ILogger<AttributeSchemasApiController> _logger = logger;

    /// <summary>
    /// Get attribute schemas for a specific workstream and attribute level.
    /// </summary>
    /// <param name="workstreamId">Workstream ID (e.g., "loans", "claims")</param>
    /// <param name="attributeLevel">Attribute level: "User", "Group", or "Role"</param>
    [HttpGet]
    public async Task<IActionResult> GetSchemas(string workstreamId, string attributeLevel)
    {
        if (string.IsNullOrWhiteSpace(workstreamId) || string.IsNullOrWhiteSpace(attributeLevel))
        {
            return BadRequest("WorkstreamId and AttributeLevel are required");
        }

        var schemas = await _schemaManagementService.GetActiveSchemasForLevelAsync(workstreamId, attributeLevel);

        var result = schemas.Select(s =>
        {
            // Parse ValidationRules to extract allowedValues if present
            List<object>? allowedValues = null;
            if (!string.IsNullOrWhiteSpace(s.ValidationRules))
            {
                try
                {
                    var validationRules = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(s.ValidationRules);
                    if (validationRules != null && validationRules.TryGetValue("allowedValues", out var allowedValuesElement))
                    {
                        allowedValues = JsonSerializer.Deserialize<List<object>>(allowedValuesElement.GetRawText());
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse ValidationRules for attribute {AttributeName}", s.AttributeName);
                }
            }

            return new
            {
                s.AttributeName,
                s.AttributeDisplayName,
                s.DataType,
                s.IsRequired,
                s.DefaultValue,
                s.ValidationRules,
                AllowedValues = allowedValues ?? new List<object>(),
                s.Description,
                s.DisplayOrder
            };
        });

        _logger.LogInformation("Retrieved {Count} attribute schemas for workstream={Workstream}, level={Level}",
            schemas.Count(), workstreamId, attributeLevel);

        return Ok(result);
    }
}
