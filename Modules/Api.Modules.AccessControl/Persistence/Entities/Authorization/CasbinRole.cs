using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Represents a role definition with metadata.
/// </summary>
[Table("CasbinRoles", Schema = "auth")]
public class CasbinRole
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Role name (e.g., "Loans.Approver", "Platform.Admin").
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string RoleName { get; set; }

    /// <summary>
    /// Workstream this role belongs to (* for global roles).
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string WorkstreamId { get; set; }

    /// <summary>
    /// Display name for UI.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Description of what this role can do.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system-defined role (cannot be deleted).
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Whether this role is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
