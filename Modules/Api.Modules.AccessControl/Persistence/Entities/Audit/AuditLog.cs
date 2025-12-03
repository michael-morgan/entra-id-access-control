using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Audit;

/// <summary>
/// Technical audit log entry from Audit.NET.
/// </summary>
[Table("AuditLogs", Schema = "audit")]
public class AuditLog
{
    [Key]
    public long AuditId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CORRELATION
    // ═══════════════════════════════════════════════════════════

    [StringLength(100)]
    public string? WorkstreamId { get; set; }

    [StringLength(50)]
    public string? RequestCorrelationId { get; set; }

    [StringLength(50)]
    public string? BusinessProcessId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // AUDIT METADATA
    // ═══════════════════════════════════════════════════════════

    [StringLength(256)]
    public string? UserId { get; set; }

    [StringLength(200)]
    public string? EntityType { get; set; }

    [StringLength(100)]
    public string? EntityId { get; set; }

    [Required]
    [StringLength(50)]
    public required string Action { get; set; }

    /// <summary>
    /// Audit data as JSON (contains old/new values, changes).
    /// </summary>
    [Required]
    public required string AuditData { get; set; }

    public required DateTimeOffset UpdatedAt { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }
}
