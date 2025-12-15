using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for group attribute management.
/// </summary>
public class GroupAttributeViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Group ID (Entra ID Object ID)")]
    public string GroupId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Workstream")]
    public string WorkstreamId { get; set; } = string.Empty;

    [Display(Name = "Group Name")]
    public string? GroupName { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Attributes (JSON)")]
    public string? AttributesJson { get; set; }

    [Display(Name = "Created At")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Display(Name = "Modified At")]
    public DateTimeOffset? ModifiedAt { get; set; }
}
