using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for Casbin policy creation/editing.
/// </summary>
public class PolicyViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Policy Type")]
    public string PolicyType { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Subject/Role")]
    public string V0 { get; set; } = string.Empty;

    [Display(Name = "Resource/Domain")]
    public string? V1 { get; set; }

    [Display(Name = "Action/Object")]
    public string? V2 { get; set; }

    [Display(Name = "Effect/Field 3")]
    public string? V3 { get; set; }

    [Display(Name = "Field 4")]
    public string? V4 { get; set; }

    [Display(Name = "Field 5")]
    public string? V5 { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
}
