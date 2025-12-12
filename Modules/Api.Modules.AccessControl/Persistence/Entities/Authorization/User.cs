using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Stores basic user information from Entra ID.
/// This table provides a local cache of user data when Graph API feature is disabled.
/// Scoped globally (not per workstream) since users exist across all workstreams.
/// </summary>
[Table("Users", Schema = "auth")]
public class User
{
    /// <summary>
    /// User ID from Entra ID (oid claim).
    /// This is the stable identifier for the user.
    /// </summary>
    [Key]
    [Required]
    [StringLength(256)]
    public required string UserId { get; set; }

    /// <summary>
    /// Display name from Entra ID token.
    /// Used for showing user-friendly names in the UI when Graph API is disabled.
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
