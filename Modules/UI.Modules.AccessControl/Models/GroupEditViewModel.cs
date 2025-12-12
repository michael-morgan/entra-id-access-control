using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for editing group information.
/// Used to manually enrich group display names and descriptions.
/// </summary>
public class GroupEditViewModel
{
    [Required]
    public required string GroupId { get; set; }

    [Required]
    [StringLength(256)]
    [Display(Name = "Display Name")]
    public required string DisplayName { get; set; }

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Source")]
    public required string Source { get; set; }

    public bool IsAutoDiscovered => Source == "JWT";
}
