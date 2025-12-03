using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Modules.AccessControl.Persistence.Entities.Authorization;

/// <summary>
/// Represents a resource definition with hierarchy support.
/// </summary>
[Table("CasbinResources", Schema = "auth")]
public class CasbinResource
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Resource pattern (e.g., "Loan", "Loan/:id", "Document/*").
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string ResourcePattern { get; set; }

    /// <summary>
    /// Workstream this resource belongs to (* for global resources).
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
    /// Description of what this resource represents.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Parent resource for hierarchy (g2 relationships).
    /// </summary>
    [StringLength(256)]
    public string? ParentResource { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAt { get; set; }

    [StringLength(256)]
    public string? CreatedBy { get; set; }

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
