using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository interface for managing attribute schemas.
/// Provides data access abstraction for attribute schema CRUD operations.
/// </summary>
public interface IAttributeSchemaRepository
{
    /// <summary>
    /// Gets all attribute schemas for a specific workstream with optional filtering.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="search">Optional search term for AttributeName</param>
    /// <param name="attributeLevel">Optional filter by attribute level (user/group/role)</param>
    /// <returns>Collection of attribute schemas matching the criteria</returns>
    Task<IEnumerable<AttributeSchema>> SearchAsync(string workstream, string? search = null, string? attributeLevel = null);

    /// <summary>
    /// Gets active attribute schemas for a specific workstream and attribute level.
    /// </summary>
    /// <param name="workstream">The workstream ID</param>
    /// <param name="attributeLevel">The attribute level (user/group/role)</param>
    /// <returns>Collection of active attribute schemas</returns>
    Task<IEnumerable<AttributeSchema>> GetActiveByLevelAsync(string workstream, string attributeLevel);

    /// <summary>
    /// Gets a single attribute schema by ID.
    /// </summary>
    /// <param name="id">The attribute schema ID</param>
    /// <returns>The attribute schema if found, null otherwise</returns>
    Task<AttributeSchema?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new attribute schema.
    /// </summary>
    /// <param name="schema">The attribute schema to create</param>
    /// <returns>The created attribute schema with ID populated</returns>
    Task<AttributeSchema> CreateAsync(AttributeSchema schema);

    /// <summary>
    /// Updates an existing attribute schema.
    /// </summary>
    /// <param name="schema">The attribute schema to update</param>
    Task UpdateAsync(AttributeSchema schema);

    /// <summary>
    /// Deletes an attribute schema by ID.
    /// </summary>
    /// <param name="id">The attribute schema ID to delete</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Checks if an attribute schema exists.
    /// </summary>
    /// <param name="id">The attribute schema ID</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(int id);
}
