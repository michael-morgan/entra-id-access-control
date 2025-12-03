using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Attributes associated with Entra ID groups for ABAC (Attribute-Based Access Control).
/// Each group can have different attributes per workstream, stored as dynamic JSON.
/// </summary>
[Table("GroupAttributes", Schema = "auth")]
public class GroupAttribute
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Entra ID Group Object ID (unique identifier).
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string GroupId { get; set; }

    /// <summary>
    /// Workstream ID - isolates attributes per business domain.
    /// Allows same group to have different attribute values in different workstreams.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string WorkstreamId { get; set; }

    /// <summary>
    /// Friendly name for the group (for display purposes).
    /// </summary>
    [StringLength(256)]
    public string? GroupName { get; set; }

    /// <summary>
    /// Whether this group is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// All custom business attributes stored as JSON.
    /// Schema defined per workstream in AttributeSchemas table.
    /// Examples: { "Department": "Finance", "Region": "APAC", "ApprovalLimit": 50000 }
    /// </summary>
    public string? AttributesJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
