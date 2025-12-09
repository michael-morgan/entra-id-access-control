using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository implementation for managing attribute schemas.
/// Provides data access for attribute schema CRUD operations.
/// </summary>
public class AttributeSchemaRepository(AccessControlDbContext context) : IAttributeSchemaRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<AttributeSchema>> SearchAsync(string workstream, string? search = null, string? attributeLevel = null)
    {
        var query = _context.AttributeSchemas
            .Where(s => s.WorkstreamId == workstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.AttributeName.Contains(search) ||
                                   s.AttributeDisplayName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(attributeLevel))
        {
            query = query.Where(s => s.AttributeLevel == attributeLevel);
        }

        return await query
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.AttributeName)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AttributeSchema>> GetActiveByLevelAsync(string workstream, string attributeLevel)
    {
        return await _context.AttributeSchemas
            .Where(s => s.WorkstreamId == workstream &&
                       s.AttributeLevel == attributeLevel &&
                       s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.AttributeName)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AttributeSchema?> GetByIdAsync(int id)
    {
        return await _context.AttributeSchemas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<AttributeSchema> CreateAsync(AttributeSchema schema)
    {
        schema.CreatedAt = DateTimeOffset.UtcNow;

        _context.AttributeSchemas.Add(schema);
        await _context.SaveChangesAsync();

        return schema;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AttributeSchema schema)
    {
        schema.ModifiedAt = DateTimeOffset.UtcNow;

        _context.AttributeSchemas.Update(schema);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var schema = await _context.AttributeSchemas.FindAsync(id);
        if (schema != null)
        {
            _context.AttributeSchemas.Remove(schema);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.AttributeSchemas.AnyAsync(s => s.Id == id);
    }
}
