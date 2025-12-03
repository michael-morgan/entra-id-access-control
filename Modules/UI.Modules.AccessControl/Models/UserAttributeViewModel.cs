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

    [Display(Name = "Attributes (JSON)")]
    public string? AttributesJson { get; set; }
}
