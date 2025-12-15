using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for user attribute management.
/// </summary>
public class UserAttributeViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "User ID (Entra ID OID)")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Workstream")]
    public string WorkstreamId { get; set; } = string.Empty;

    [Display(Name = "User Name")]
    public string? UserName { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Attributes (JSON)")]
    public string? AttributesJson { get; set; }

    [Display(Name = "Created At")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Display(Name = "Modified At")]
    public DateTimeOffset? ModifiedAt { get; set; }
}
