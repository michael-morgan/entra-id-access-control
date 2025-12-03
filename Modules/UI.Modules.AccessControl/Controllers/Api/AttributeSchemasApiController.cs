using Api.Modules.AccessControl.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UI.Modules.AccessControl.Controllers.Api;

[ApiController]
[Route("api/attribute-schemas")]
public class AttributeSchemasApiController : ControllerBase
{
    private readonly AccessControlDbContext _context;
    private readonly ILogger<AttributeSchemasApiController> _logger;

    public AttributeSchemasApiController(AccessControlDbContext context, ILogger<AttributeSchemasApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

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

        var schemas = await _context.AttributeSchemas
            .Where(s => s.WorkstreamId == workstreamId && s.AttributeLevel == attributeLevel && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.AttributeName)
            .Select(s => new
            {
                s.AttributeName,
                s.AttributeDisplayName,
                s.DataType,
                s.IsRequired,
                s.DefaultValue,
                s.ValidationRules,
                s.Description,
                s.DisplayOrder
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} attribute schemas for workstream={Workstream}, level={Level}",
            schemas.Count, workstreamId, attributeLevel);

        return Ok(schemas);
    }
}
