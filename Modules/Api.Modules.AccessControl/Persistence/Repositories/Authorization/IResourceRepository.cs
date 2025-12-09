using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository interface for managing Casbin resources.
/// Provides data access abstraction for resource CRUD operations.
/// </summary>
public interface IResourceRepository
{
    /// <summary>
    /// Gets all resources for a specific workstream with optional filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID to filter by (includes global resources with "*")</param>
    /// <param name="workstreamFilter">Optional additional workstream filter (exact match)</param>
    /// <param name="search">Optional search term for ResourcePattern, DisplayName, or Description</param>
    /// <returns>Collection of resources matching the criteria</returns>
    Task<IEnumerable<CasbinResource>> SearchAsync(
        string workstream,
        string? workstreamFilter = null,
        string? search = null);

    /// <summary>
    /// Gets a single resource by its ID.
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <returns>The resource if found, null otherwise</returns>
    Task<CasbinResource?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a resource by pattern and workstream.
    /// </summary>
    /// <param name="resourcePattern">The resource pattern</param>
    /// <param name="workstream">The workstream ID</param>
    /// <returns>The resource if found, null otherwise</returns>
    Task<CasbinResource?> GetByPatternAsync(string resourcePattern, string workstream);

    /// <summary>
    /// Creates a new resource.
    /// </summary>
    /// <param name="resource">The resource to create</param>
    /// <returns>The created resource with ID populated</returns>
    Task<CasbinResource> CreateAsync(CasbinResource resource);

    /// <summary>
    /// Updates an existing resource.
    /// </summary>
    /// <param name="resource">The resource to update</param>
    Task UpdateAsync(CasbinResource resource);

    /// <summary>
    /// Deletes a resource by ID.
    /// </summary>
    /// <param name="id">The resource ID to delete</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Checks if a resource exists by ID.
    /// </summary>
    /// <param name="id">The resource ID to check</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(int id);
}
