using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for creating and editing Attribute Schemas.
/// </summary>
public class AttributeSchemaViewModel
{
    public int Id { get; set; }

    [StringLength(50)]
    [Display(Name = "Workstream ID")]
    public string? WorkstreamId { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Attribute Level")]
    public string AttributeLevel { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Attribute Name")]
    public string AttributeName { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    [Display(Name = "Display Name")]
    public string AttributeDisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Data Type")]
    public string DataType { get; set; } = string.Empty;

    [Display(Name = "Is Required")]
    public bool IsRequired { get; set; }

    [Display(Name = "Default Value")]
    public string? DefaultValue { get; set; }

    [Display(Name = "Validation Rules (JSON)")]
    public string? ValidationRules { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Display Order")]
    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created At")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Modified At")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Display(Name = "Modified By")]
    public string? ModifiedBy { get; set; }

    // Helper property for allowed values UI (comma-separated list)
    [Display(Name = "Allowed Values (comma-separated)")]
    public string? AllowedValuesInput { get; set; }
}
