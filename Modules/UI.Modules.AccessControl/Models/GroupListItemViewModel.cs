namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for displaying a group in a list.
/// </summary>
public class GroupListItemViewModel
{
    public required string GroupId { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required string Source { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int UserCount { get; set; }
    public bool NeedsEnrichment => DisplayName == GroupId;
}
