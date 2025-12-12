using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Represents the many-to-many relationship between Users and Groups.
/// Tracks when group memberships were last observed in JWT tokens.
/// </summary>
[Table("UserGroups", Schema = "auth")]
public class UserGroup
{
    /// <summary>
    /// Primary key (auto-increment).
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User ID (Entra OID). Foreign key to Users table.
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string UserId { get; set; }

    /// <summary>
    /// Group ID (Entra OID). Foreign key to Groups table.
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string GroupId { get; set; }

    /// <summary>
    /// When this group membership was first discovered/created.
    /// </summary>
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this group membership was last observed in a JWT token.
    /// Updated each time the user makes a request with this group in their JWT.
    /// Used to detect stale associations (user removed from group in Entra ID).
    /// </summary>
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Source of this association record.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Source { get; set; } // "JWT", "Manual"

    // Navigation properties
    public User? User { get; set; }
    public Group? Group { get; set; }
}

/// <summary>
/// Source of a user-group association.
/// </summary>
public static class UserGroupSource
{
    public const string JWT = "JWT";
    public const string Manual = "Manual";
}
