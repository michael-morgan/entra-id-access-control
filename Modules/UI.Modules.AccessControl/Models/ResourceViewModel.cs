using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for Casbin resource definition creation/editing.
/// </summary>
public class ResourceViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(256)]
    [Display(Name = "Resource Pattern")]
    public string ResourcePattern { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Workstream ID")]
    public string WorkstreamId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(256)]
    [Display(Name = "Parent Resource")]
    public string? ParentResource { get; set; }

    [Display(Name = "Created At")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Modified At")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Display(Name = "Modified By")]
    public string? ModifiedBy { get; set; }
}
