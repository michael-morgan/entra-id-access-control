using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository implementation for managing Casbin resources.
/// Provides data access for resource CRUD operations.
/// </summary>
public class ResourceRepository(AccessControlDbContext context) : IResourceRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinResource>> SearchAsync(
        string workstream,
        string? workstreamFilter = null,
        string? search = null)
    {
        var query = _context.CasbinResources
            .Where(r => r.WorkstreamId == workstream || r.WorkstreamId == "*");

        if (!string.IsNullOrWhiteSpace(workstreamFilter))
        {
            query = query.Where(r => r.WorkstreamId == workstreamFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.ResourcePattern.Contains(search) ||
                r.DisplayName.Contains(search) ||
                (r.Description != null && r.Description.Contains(search)));
        }

        return await query
            .OrderBy(r => r.WorkstreamId)
            .ThenBy(r => r.ResourcePattern)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CasbinResource?> GetByIdAsync(int id)
    {
        return await _context.CasbinResources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<CasbinResource?> GetByPatternAsync(string resourcePattern, string workstream)
    {
        return await _context.CasbinResources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourcePattern == resourcePattern && r.WorkstreamId == workstream);
    }

    /// <inheritdoc />
    public async Task<CasbinResource> CreateAsync(CasbinResource resource)
    {
        resource.CreatedAt = DateTimeOffset.UtcNow;

        _context.CasbinResources.Add(resource);
        await _context.SaveChangesAsync();

        return resource;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CasbinResource resource)
    {
        resource.ModifiedAt = DateTimeOffset.UtcNow;

        _context.CasbinResources.Update(resource);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var resource = await _context.CasbinResources.FindAsync(id);
        if (resource != null)
        {
            _context.CasbinResources.Remove(resource);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.CasbinResources
            .AsNoTracking()
            .AnyAsync(r => r.Id == id);
    }
}
