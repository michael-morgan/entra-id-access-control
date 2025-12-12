using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for displaying detailed group information.
/// </summary>
public class GroupDetailsViewModel
{
    public required string GroupId { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required string Source { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool NeedsEnrichment => DisplayName == GroupId;

    /// <summary>
    /// List of users who are members of this group.
    /// </summary>
    public List<UserGroup> Members { get; set; } = [];
}
