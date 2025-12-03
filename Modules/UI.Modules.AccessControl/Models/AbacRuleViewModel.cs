using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for creating and editing ABAC rules.
/// </summary>
public class AbacRuleViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Workstream ID")]
    public string WorkstreamId { get; set; } = string.Empty;

    [Display(Name = "Rule Group")]
    public int? RuleGroupId { get; set; }

    [Display(Name = "Rule Group Name")]
    public string? RuleGroupName { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Rule Name")]
    public string RuleName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Rule Type")]
    public string RuleType { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Configuration (JSON)")]
    public string Configuration { get; set; } = string.Empty;

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Priority")]
    [Range(0, int.MaxValue)]
    public int Priority { get; set; }

    [StringLength(500)]
    [Display(Name = "Failure Message")]
    public string? FailureMessage { get; set; }

    [Display(Name = "Created At")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Display(Name = "Modified At")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Modified By")]
    public string? ModifiedBy { get; set; }
}
