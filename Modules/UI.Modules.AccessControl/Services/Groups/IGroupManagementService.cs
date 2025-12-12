using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Groups;

/// <summary>
/// Service for managing groups in the admin UI.
/// </summary>
public interface IGroupManagementService
{
    /// <summary>
    /// Get all groups with user counts.
    /// </summary>
    Task<List<GroupListItemViewModel>> GetAllGroupsAsync();

    /// <summary>
    /// Get detailed information about a specific group.
    /// </summary>
    Task<GroupDetailsViewModel?> GetGroupDetailsAsync(string groupId);

    /// <summary>
    /// Update group display name and description (manual enrichment).
    /// </summary>
    Task<bool> UpdateGroupAsync(string groupId, string displayName, string? description);

    /// <summary>
    /// Get stale user-group associations (not seen in JWT recently).
    /// </summary>
    Task<List<StaleAssociationViewModel>> GetStaleAssociationsAsync(int daysThreshold);

    /// <summary>
    /// Mark a stale association as "Manual" to keep it (convert from JWT to Manual source).
    /// </summary>
    Task<bool> KeepStaleAssociationAsync(int associationId);

    /// <summary>
    /// Remove a stale association.
    /// </summary>
    Task<bool> RemoveStaleAssociationAsync(int associationId);
}
