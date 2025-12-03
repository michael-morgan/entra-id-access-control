using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

public class AbacRuleGroupViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Workstream ID")]
    public string WorkstreamId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Group Name")]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Parent Group")]
    public int? ParentGroupId { get; set; }

    [Required]
    [Display(Name = "Logical Operator")]
    public string LogicalOperator { get; set; } = "AND";

    [StringLength(100)]
    public string? Resource { get; set; }

    [StringLength(50)]
    public string? Action { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public int Priority { get; set; }

    // For display
    public string? ParentGroupName { get; set; }
    public int ChildGroupCount { get; set; }
    public int RuleCount { get; set; }
}
