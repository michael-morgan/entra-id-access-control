using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Events;

/// <summary>
/// Stored business event in the immutable event store.
/// NO hash chain fields - immutability enforced by SQL trigger.
/// </summary>
[Table("BusinessEvents", Schema = "events")]
public class StoredBusinessEvent
{
    [Key]
    public Guid EventId { get; set; }

    /// <summary>
    /// Auto-incrementing sequence number for global ordering.
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long SequenceNumber { get; set; }

    // ═══════════════════════════════════════════════════════════
    // CORRELATION
    // ═══════════════════════════════════════════════════════════

    [StringLength(50)]
    public string? BusinessProcessId { get; set; }

    [StringLength(50)]
    public string? SessionCorrelationId { get; set; }

    [Required]
    [StringLength(50)]
    public required string RequestCorrelationId { get; set; }

    [Required]
    [StringLength(100)]
    public required string WorkstreamId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // EVENT IDENTITY
    // ═══════════════════════════════════════════════════════════

    [Required]
    [StringLength(200)]
    public required string EventType { get; set; }

    [Required]
    [StringLength(100)]
    public required string EventCategory { get; set; }

    public int EventVersion { get; set; } = 1;

    // ═══════════════════════════════════════════════════════════
    // ACTOR
    // ═══════════════════════════════════════════════════════════

    [Required]
    [StringLength(256)]
    public required string ActorId { get; set; }

    [Required]
    [StringLength(50)]
    public required string ActorType { get; set; }

    [StringLength(200)]
    public string? ActorDisplayName { get; set; }

    [StringLength(50)]
    public string? ActorIpAddress { get; set; }

    // ═══════════════════════════════════════════════════════════
    // TEMPORAL
    // ═══════════════════════════════════════════════════════════

    public required DateTimeOffset OccurredAt { get; set; }

    public required DateTimeOffset RecordedAt { get; set; }

    // ═══════════════════════════════════════════════════════════
    // PAYLOAD
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Event data as JSON.
    /// </summary>
    [Required]
    public required string EventData { get; set; }

    /// <summary>
    /// Justification for the action (for approvals, overrides).
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Affected entities as JSON array.
    /// </summary>
    public string? AffectedEntities { get; set; }

    // Navigation property
    [ForeignKey(nameof(BusinessProcessId))]
    public BusinessProcessEntity? BusinessProcess { get; set; }
}
