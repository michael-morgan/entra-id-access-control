using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for Casbin role creation/editing.
/// </summary>
public class RoleViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Role Name")]
    public string RoleName { get; set; } = string.Empty;

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

    [Display(Name = "System Role")]
    public bool IsSystemRole { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created At")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Modified At")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Display(Name = "Modified By")]
    public string? ModifiedBy { get; set; }
}
