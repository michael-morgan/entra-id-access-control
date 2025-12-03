using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Attributes associated with Entra ID Roles for fine-grained ABAC.
/// Each role can have different attributes per workstream, stored as dynamic JSON.
/// </summary>
[Table("RoleAttributes", Schema = "auth")]
public class RoleAttribute
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Entra ID App Role ID (unique identifier from app registration).
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string AppRoleId { get; set; }

    /// <summary>
    /// Role value (e.g., "Approver.Senior", "Viewer.ReadOnly").
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string RoleValue { get; set; }

    /// <summary>
    /// Workstream ID - isolates attributes per business domain.
    /// Allows same role to have different attribute values in different workstreams.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string WorkstreamId { get; set; }

    /// <summary>
    /// Friendly display name for the role.
    /// </summary>
    [StringLength(256)]
    public string? RoleDisplayName { get; set; }

    /// <summary>
    /// Whether this role is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// All custom business attributes stored as JSON.
    /// Schema defined per workstream in AttributeSchemas table.
    /// Examples: { "ManagementLevel": 5, "ApprovalLimit": 100000, "TransactionLimit": 250000 }
    /// </summary>
    public string? AttributesJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
