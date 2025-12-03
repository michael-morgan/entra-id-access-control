using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Groups ABAC rules together with AND/OR logic for complex authorization scenarios.
/// Supports nested groups for sophisticated rule combinations.
/// </summary>
[Table("AbacRuleGroups", Schema = "auth")]
public class AbacRuleGroup
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Workstream ID this rule group applies to.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string WorkstreamId { get; set; }

    /// <summary>
    /// Unique name for this rule group (for debugging and UI).
    /// Example: "HighValueLoanApproval", "RegionalAccessControl", "SeniorManagerRestrictions"
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string GroupName { get; set; }

    /// <summary>
    /// Description of what this rule group enforces.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Parent group ID for nested groups (null for top-level groups).
    /// </summary>
    public int? ParentGroupId { get; set; }

    /// <summary>
    /// Logical operator for combining rules/groups within this group.
    /// Values: 'AND', 'OR'
    /// </summary>
    [Required]
    [StringLength(10)]
    public required string LogicalOperator { get; set; }

    /// <summary>
    /// Which Casbin resource/domain this rule group applies to.
    /// Example: "loans", "claims", "documents"
    /// </summary>
    [StringLength(100)]
    public string? Resource { get; set; }

    /// <summary>
    /// Which action this rule group applies to.
    /// Example: "approve", "view", "edit", "delete"
    /// </summary>
    [StringLength(50)]
    public string? Action { get; set; }

    /// <summary>
    /// Whether this rule group is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority/order for evaluation (lower = evaluated first).
    /// </summary>
    public int Priority { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ParentGroupId))]
    public AbacRuleGroup? ParentGroup { get; set; }

    public ICollection<AbacRuleGroup> ChildGroups { get; set; } = new List<AbacRuleGroup>();
    public ICollection<AbacRule> Rules { get; set; } = new List<AbacRule>();
}
