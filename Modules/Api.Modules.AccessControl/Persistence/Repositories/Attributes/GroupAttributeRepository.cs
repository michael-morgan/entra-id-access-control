using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Attributes;

/// <summary>
/// Repository implementation for managing group attributes.
/// Provides data access for group attribute CRUD operations.
/// </summary>
public class GroupAttributeRepository(AccessControlDbContext context) : IGroupAttributeRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<GroupAttribute>> SearchAsync(string workstream, string? search = null)
    {
        var query = _context.GroupAttributes
            .Where(ga => ga.WorkstreamId == workstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ga =>
                ga.GroupId.Contains(search) ||
                (ga.GroupName != null && ga.GroupName.Contains(search)));
        }

        return await query
            .OrderBy(ga => ga.GroupName ?? ga.GroupId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<GroupAttribute?> GetByIdAsync(int id)
    {
        return await _context.GroupAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(ga => ga.Id == id);
    }

    /// <inheritdoc />
    public async Task<GroupAttribute?> GetByGroupIdAndWorkstreamAsync(string groupId, string workstream)
    {
        return await _context.GroupAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(ga => ga.GroupId == groupId && ga.WorkstreamId == workstream);
    }

    /// <inheritdoc />
    public async Task<GroupAttribute> CreateAsync(GroupAttribute groupAttribute)
    {
        groupAttribute.CreatedAt = DateTimeOffset.UtcNow;
        groupAttribute.IsActive = true;

        _context.GroupAttributes.Add(groupAttribute);
        await _context.SaveChangesAsync();

        return groupAttribute;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(GroupAttribute groupAttribute)
    {
        groupAttribute.ModifiedAt = DateTimeOffset.UtcNow;

        _context.GroupAttributes.Update(groupAttribute);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateManyAsync(IEnumerable<GroupAttribute> groupAttributes)
    {
        foreach (var groupAttribute in groupAttributes)
        {
            groupAttribute.ModifiedAt = DateTimeOffset.UtcNow;
            _context.GroupAttributes.Update(groupAttribute);
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var groupAttribute = await _context.GroupAttributes.FindAsync(id);
        if (groupAttribute != null)
        {
            _context.GroupAttributes.Remove(groupAttribute);
            await _context.SaveChangesAsync();
        }
    }
}
