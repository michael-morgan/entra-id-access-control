using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Authorization.Resources;

/// <summary>
/// Service interface for managing Casbin resources with business logic.
/// Orchestrates repository calls and ViewModel mapping.
/// </summary>
public interface IResourceManagementService
{
    /// <summary>
    /// Gets resources for a workstream with optional filtering.
    /// </summary>
    /// <param name="selectedWorkstream">The selected workstream</param>
    /// <param name="workstreamFilter">Optional workstream filter</param>
    /// <param name="search">Optional search term</param>
    /// <returns>Collection of resources matching the criteria</returns>
    Task<IEnumerable<CasbinResource>> GetResourcesAsync(
        string selectedWorkstream,
        string? workstreamFilter = null,
        string? search = null);

    /// <summary>
    /// Gets a single resource by ID.
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <returns>The resource if found, null otherwise</returns>
    Task<CasbinResource?> GetResourceByIdAsync(int id);

    /// <summary>
    /// Creates a new resource with validation.
    /// </summary>
    /// <param name="model">The resource view model</param>
    /// <param name="createdBy">The user creating the resource</param>
    /// <returns>Tuple of (success, resource, errorMessage)</returns>
    Task<(bool Success, CasbinResource? Resource, string? ErrorMessage)> CreateResourceAsync(
        ResourceViewModel model, string createdBy);

    /// <summary>
    /// Updates an existing resource.
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <param name="model">The updated resource view model</param>
    /// <param name="modifiedBy">The user modifying the resource</param>
    /// <returns>Tuple of (success, errorMessage)</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateResourceAsync(
        int id, ResourceViewModel model, string modifiedBy);

    /// <summary>
    /// Deletes a resource.
    /// </summary>
    /// <param name="id">The resource ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteResourceAsync(int id);
}
