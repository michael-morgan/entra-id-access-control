using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Attributes;

/// <summary>
/// Service interface for managing group attributes with business logic.
/// Orchestrates repository calls, Graph API sync, and ViewModel mapping.
/// </summary>
public interface IGroupAttributeManagementService
{
    /// <summary>
    /// Gets group attributes for a workstream with optional search filtering.
    /// Automatically syncs group display names from Graph API.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term</param>
    /// <returns>Collection of group attributes with synced display names</returns>
    Task<IEnumerable<GroupAttribute>> GetGroupAttributesAsync(string workstream, string? search = null);

    /// <summary>
    /// Gets a single group attribute by ID.
    /// </summary>
    /// <param name="id">The group attribute ID</param>
    /// <returns>The group attribute if found, null otherwise</returns>
    Task<GroupAttribute?> GetGroupAttributeByIdAsync(int id);

    /// <summary>
    /// Creates a new group attribute with validation.
    /// </summary>
    /// <param name="model">The group attribute view model</param>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>Tuple of (success, groupAttribute, errorMessage)</returns>
    Task<(bool Success, GroupAttribute? GroupAttribute, string? ErrorMessage)> CreateGroupAttributeAsync(
        GroupAttributeViewModel model, string workstream);

    /// <summary>
    /// Updates an existing group attribute.
    /// </summary>
    /// <param name="id">The group attribute ID</param>
    /// <param name="model">The updated group attribute view model</param>
    /// <returns>Tuple of (success, errorMessage)</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateGroupAttributeAsync(
        int id, GroupAttributeViewModel model);

    /// <summary>
    /// Deletes a group attribute.
    /// </summary>
    /// <param name="id">The group attribute ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteGroupAttributeAsync(int id);
}
