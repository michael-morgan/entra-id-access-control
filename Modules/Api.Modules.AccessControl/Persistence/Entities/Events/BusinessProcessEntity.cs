using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Events;

/// <summary>
/// Represents a long-lived business process.
/// </summary>
[Table("BusinessProcesses", Schema = "events")]
public class BusinessProcessEntity
{
    [Key]
    [StringLength(50)]
    public required string BusinessProcessId { get; set; }

    [Required]
    [StringLength(100)]
    public required string ProcessType { get; set; }

    [Required]
    [StringLength(100)]
    public required string WorkstreamId { get; set; }

    [Required]
    [StringLength(50)]
    public required string Status { get; set; }

    [Required]
    [StringLength(256)]
    public required string InitiatedBy { get; set; }

    public required DateTimeOffset InitiatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    [StringLength(50)]
    public string? Outcome { get; set; }

    /// <summary>
    /// JSON metadata about the process.
    /// </summary>
    public string? Metadata { get; set; }
}
