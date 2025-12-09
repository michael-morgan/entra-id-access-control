using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository implementation for managing Casbin roles.
/// Provides data access for role CRUD operations.
/// </summary>
public class RoleRepository(AccessControlDbContext context) : IRoleRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinRole>> SearchAsync(
        string workstream,
        string? search = null)
    {
        var query = _context.CasbinRoles
            .Where(r => r.WorkstreamId == workstream || r.WorkstreamId == "*");

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.RoleName.Contains(search) ||
                r.DisplayName.Contains(search) ||
                (r.Description != null && r.Description.Contains(search)));
        }

        return await query
            .OrderBy(r => r.WorkstreamId)
            .ThenBy(r => r.RoleName)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CasbinRole?> GetByIdAsync(int id)
    {
        return await _context.CasbinRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<CasbinRole?> GetByRoleNameAsync(string roleName, string workstream)
    {
        return await _context.CasbinRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleName == roleName && r.WorkstreamId == workstream);
    }

    /// <inheritdoc />
    public async Task<bool> IsDuplicateRoleNameAsync(string roleName, string workstream, int? excludeId = null)
    {
        var query = _context.CasbinRoles
            .Where(r => r.RoleName == roleName && r.WorkstreamId == workstream);

        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }

        return await query.AsNoTracking().AnyAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsReferencedInPoliciesAsync(string roleName)
    {
        return await _context.CasbinPolicies
            .AsNoTracking()
            .AnyAsync(p => p.V0 == roleName || p.V1 == roleName);
    }

    /// <inheritdoc />
    public async Task<CasbinRole> CreateAsync(CasbinRole role)
    {
        role.CreatedAt = DateTimeOffset.UtcNow;
        role.IsActive = true;

        _context.CasbinRoles.Add(role);
        await _context.SaveChangesAsync();

        return role;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CasbinRole role)
    {
        role.ModifiedAt = DateTimeOffset.UtcNow;

        _context.CasbinRoles.Update(role);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var role = await _context.CasbinRoles.FindAsync(id);
        if (role != null)
        {
            _context.CasbinRoles.Remove(role);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.CasbinRoles
            .AsNoTracking()
            .AnyAsync(r => r.Id == id);
    }
}
