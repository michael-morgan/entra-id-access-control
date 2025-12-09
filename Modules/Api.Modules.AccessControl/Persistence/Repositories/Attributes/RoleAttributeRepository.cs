using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Attributes;

/// <summary>
/// Repository implementation for managing role attributes.
/// Provides data access for role attribute CRUD operations.
/// </summary>
public class RoleAttributeRepository(AccessControlDbContext context) : IRoleAttributeRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<RoleAttribute>> SearchAsync(string workstream, string? search = null)
    {
        var query = _context.RoleAttributes
            .Where(ra => ra.WorkstreamId == workstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ra => ra.RoleValue.Contains(search) ||
                                    (ra.RoleDisplayName != null && ra.RoleDisplayName.Contains(search)));
        }

        return await query
            .OrderBy(ra => ra.RoleDisplayName ?? ra.RoleValue)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<RoleAttribute?> GetByIdAsync(int id)
    {
        return await _context.RoleAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(ra => ra.Id == id);
    }

    /// <inheritdoc />
    public async Task<RoleAttribute?> GetByRoleAndWorkstreamAsync(string appRoleId, string roleValue, string workstream)
    {
        return await _context.RoleAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(ra => ra.AppRoleId == appRoleId &&
                                      ra.RoleValue == roleValue &&
                                      ra.WorkstreamId == workstream);
    }

    /// <inheritdoc />
    public async Task<RoleAttribute> CreateAsync(RoleAttribute roleAttribute)
    {
        roleAttribute.CreatedAt = DateTimeOffset.UtcNow;

        _context.RoleAttributes.Add(roleAttribute);
        await _context.SaveChangesAsync();

        return roleAttribute;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RoleAttribute roleAttribute)
    {
        roleAttribute.ModifiedAt = DateTimeOffset.UtcNow;

        _context.RoleAttributes.Update(roleAttribute);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var roleAttribute = await _context.RoleAttributes.FindAsync(id);
        if (roleAttribute != null)
        {
            _context.RoleAttributes.Remove(roleAttribute);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.RoleAttributes.AnyAsync(ra => ra.Id == id);
    }
}
