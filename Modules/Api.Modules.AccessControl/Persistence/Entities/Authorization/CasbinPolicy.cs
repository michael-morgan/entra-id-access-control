using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Represents a Casbin policy rule or role assignment.
/// Stores both 'p' (policy) and 'g' (grouping/role) rules.
/// </summary>
[Table("CasbinPolicies", Schema = "auth")]
public class CasbinPolicy
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Policy type: 'p' for policy rules, 'g' for role assignments, 'g2' for resource hierarchy.
    /// </summary>
    [Required]
    [StringLength(10)]
    public required string PolicyType { get; set; }

    /// <summary>
    /// First value: subject (user/role) for 'p', user for 'g', child resource for 'g2'.
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string V0 { get; set; }

    /// <summary>
    /// Second value: workstream for 'p', role for 'g', parent resource for 'g2'.
    /// </summary>
    [StringLength(256)]
    public string? V1 { get; set; }

    /// <summary>
    /// Third value: resource for 'p', workstream for 'g'.
    /// </summary>
    [StringLength(256)]
    public string? V2 { get; set; }

    /// <summary>
    /// Fourth value: action for 'p'.
    /// </summary>
    [StringLength(256)]
    public string? V3 { get; set; }

    /// <summary>
    /// Fifth value: effect (allow/deny) for 'p'.
    /// </summary>
    [StringLength(10)]
    public string? V4 { get; set; }

    /// <summary>
    /// Sixth value: reserved for future use.
    /// </summary>
    [StringLength(256)]
    public string? V5 { get; set; }

    /// <summary>
    /// Whether this policy is active.
    /// Soft delete by setting to false.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Workstream ID for filtering policies by workstream.
    /// </summary>
    [StringLength(100)]
    public string? WorkstreamId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
