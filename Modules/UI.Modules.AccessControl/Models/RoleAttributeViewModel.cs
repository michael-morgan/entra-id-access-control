using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for role attribute management.
/// </summary>
public class RoleAttributeViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "App Role ID (Entra ID)")]
    public string AppRoleId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role Value")]
    public string RoleValue { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Workstream")]
    public string WorkstreamId { get; set; } = string.Empty;

    [Display(Name = "Role Display Name")]
    public string? RoleDisplayName { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Attributes (JSON)")]
    public string? AttributesJson { get; set; }
}
