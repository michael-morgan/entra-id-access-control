using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Defines the schema for custom attributes per workstream.
/// Used by Admin.Web to dynamically generate forms for attribute management.
/// </summary>
[Table("AttributeSchemas", Schema = "auth")]
public class AttributeSchema
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Workstream ID this schema applies to.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string WorkstreamId { get; set; }

    /// <summary>
    /// Which level this attribute applies to: 'Group', 'Role', or 'User'.
    /// </summary>
    [Required]
    [StringLength(20)]
    public required string AttributeLevel { get; set; }

    /// <summary>
    /// JSON property name for this attribute.
    /// Example: "ApprovalLimit", "Department", "Region"
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string AttributeName { get; set; }

    /// <summary>
    /// Friendly display name for UI.
    /// Example: "Maximum Approval Limit", "Department Name", "Geographic Region"
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string AttributeDisplayName { get; set; }

    /// <summary>
    /// Data type for validation and UI rendering.
    /// Values: 'String', 'Number', 'Boolean', 'Array', 'Object', 'Date'
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string DataType { get; set; }

    /// <summary>
    /// Whether this attribute is required when creating/editing.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Default value for new entries (JSON-encoded).
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Validation rules as JSON.
    /// Example: { "min": 0, "max": 1000000, "pattern": "^[A-Z]{2,4}$" }
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// Help text/description for this attribute.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Display order in forms (lower = appears first).
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this schema definition is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
