using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Attributes;

/// <summary>
/// Service interface for managing user attributes with business logic.
/// Orchestrates repository calls, Graph API lookups, and ViewModel mapping.
/// </summary>
public interface IUserAttributeManagementService
{
    /// <summary>
    /// Gets user attributes for a workstream with optional search filtering.
    /// Enriches with user display names from Graph API.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term</param>
    /// <returns>Tuple of (userAttributes, userDisplayNames)</returns>
    Task<(IEnumerable<UserAttribute> UserAttributes, Dictionary<string, string> UserDisplayNames)>
        GetUserAttributesWithDisplayNamesAsync(string workstream, string? search = null);

    /// <summary>
    /// Gets a single user attribute by ID with display name.
    /// </summary>
    /// <param name="id">The user attribute ID</param>
    /// <returns>Tuple of (userAttribute, userDisplayName) or null if not found</returns>
    Task<(UserAttribute? UserAttribute, string? UserDisplayName)?> GetUserAttributeByIdWithDisplayNameAsync(int id);

    /// <summary>
    /// Creates a new user attribute with validation.
    /// </summary>
    /// <param name="model">The user attribute view model</param>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>Tuple of (success, userAttribute, errorMessage)</returns>
    Task<(bool Success, UserAttribute? UserAttribute, string? ErrorMessage)> CreateUserAttributeAsync(
        UserAttributeViewModel model, string workstream);

    /// <summary>
    /// Updates an existing user attribute.
    /// </summary>
    /// <param name="id">The user attribute ID</param>
    /// <param name="model">The updated user attribute view model</param>
    /// <returns>Tuple of (success, errorMessage)</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateUserAttributeAsync(
        int id, UserAttributeViewModel model);

    /// <summary>
    /// Deletes a user attribute.
    /// </summary>
    /// <param name="id">The user attribute ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteUserAttributeAsync(int id);
}
