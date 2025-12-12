using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Represents an Entra ID (Azure AD) security group.
/// Groups are discovered from JWT tokens or manually seeded.
/// Display names can be enriched via admin UI.
/// </summary>
[Table("Groups", Schema = "auth")]
public class Group
{
    /// <summary>
    /// Entra ID group object ID (OID). This is the primary key.
    /// </summary>
    [Key]
    [Required]
    [StringLength(256)]
    public required string GroupId { get; set; }

    /// <summary>
    /// Friendly display name for the group.
    /// Defaults to GroupId (OID) if not enriched.
    /// Can be updated via admin UI.
    /// </summary>
    [StringLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Optional description of the group's purpose.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Source of this group record.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Source { get; set; } // "JWT", "Manual", "GraphAPI"

    /// <summary>
    /// When this group record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this group record was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// User ID who created this record.
    /// </summary>
    [StringLength(256)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User ID who last modified this record.
    /// </summary>
    [StringLength(256)]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    public ICollection<UserGroup> UserGroups { get; set; } = [];
}

/// <summary>
/// Source of a group record.
/// </summary>
public static class GroupSource
{
    public const string JWT = "JWT";
    public const string Manual = "Manual";
    public const string GraphAPI = "GraphAPI";
}
