namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for displaying stale user-group associations.
/// </summary>
public class StaleAssociationViewModel
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public required string GroupId { get; set; }
    public required string GroupDisplayName { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public required string Source { get; set; }
    public int DaysSinceLastSeen { get; set; }
}
