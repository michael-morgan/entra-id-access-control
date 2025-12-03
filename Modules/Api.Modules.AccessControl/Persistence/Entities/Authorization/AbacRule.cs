using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Declarative ABAC rules for common authorization patterns.
/// Evaluated alongside custom code-based evaluators.
/// </summary>
[Table("AbacRules", Schema = "auth")]
public class AbacRule
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Workstream ID this rule applies to.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string WorkstreamId { get; set; }

    /// <summary>
    /// Optional rule group this rule belongs to.
    /// Null for standalone rules.
    /// </summary>
    public int? RuleGroupId { get; set; }

    /// <summary>
    /// Unique name for this rule (for debugging and UI).
    /// Example: "ApprovalLimitCheck", "RegionMatchRequired", "BusinessHoursOnly"
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string RuleName { get; set; }

    /// <summary>
    /// Type of rule evaluation.
    /// Values: 'AttributeComparison', 'PropertyMatch', 'ValueRange', 'TimeRestriction', 'LocationRestriction', 'AttributeValue'
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string RuleType { get; set; }

    /// <summary>
    /// Rule configuration as JSON.
    /// Structure depends on RuleType:
    /// - AttributeComparison: { "userAttribute": "ApprovalLimit", "operator": ">=", "resourceProperty": "Amount" }
    /// - PropertyMatch: { "userAttribute": "Region", "operator": "==", "resourceProperty": "Region" }
    /// - ValueRange: { "resourceProperty": "Amount", "min": 0, "max": 10000 }
    /// - TimeRestriction: { "allowedHours": { "start": 9, "end": 17 }, "timezone": "America/New_York" }
    /// - LocationRestriction: { "allowedNetworks": ["10.0.0.0/8", "192.168.1.0/24"] }
    /// </summary>
    public required string Configuration { get; set; }

    /// <summary>
    /// Whether this rule is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority/order for evaluation (lower = evaluated first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Error message to return when rule fails.
    /// </summary>
    [StringLength(500)]
    public string? FailureMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RuleGroupId))]
    public AbacRuleGroup? RuleGroup { get; set; }
}
