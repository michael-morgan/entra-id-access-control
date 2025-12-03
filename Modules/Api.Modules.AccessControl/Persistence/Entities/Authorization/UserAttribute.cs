using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Stores user-specific attributes for ABAC evaluation.
/// Each user can have different attributes per workstream, stored as dynamic JSON.
/// </summary>
[Table("UserAttributes", Schema = "auth")]
public class UserAttribute
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User ID from Entra ID (oid claim).
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string UserId { get; set; }

    /// <summary>
    /// Workstream ID - isolates attributes per business domain.
    /// Allows same user to have different attribute overrides in different workstreams.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string WorkstreamId { get; set; }

    /// <summary>
    /// All custom business attributes stored as JSON.
    /// Schema defined per workstream in AttributeSchemas table.
    /// These override group and role attributes (highest precedence).
    /// Examples: { "Department": "Finance", "ApprovalLimit": 500000, "ManagementLevel": 7 }
    /// </summary>
    public string? AttributesJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
