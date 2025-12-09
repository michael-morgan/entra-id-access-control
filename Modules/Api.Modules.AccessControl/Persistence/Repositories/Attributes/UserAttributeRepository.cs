using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Attributes;

/// <summary>
/// Repository implementation for managing user attributes.
/// Provides data access for user attribute CRUD operations.
/// </summary>
public class UserAttributeRepository(AccessControlDbContext context) : IUserAttributeRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<UserAttribute>> SearchAsync(string workstream, string? search = null)
    {
        var query = _context.UserAttributes
            .Where(ua => ua.WorkstreamId == workstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ua => ua.UserId.Contains(search));
        }

        return await query
            .OrderBy(ua => ua.UserId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UserAttribute?> GetByIdAsync(int id)
    {
        return await _context.UserAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(ua => ua.Id == id);
    }

    /// <inheritdoc />
    public async Task<UserAttribute?> GetByUserIdAndWorkstreamAsync(string userId, string workstream)
    {
        return await _context.UserAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.WorkstreamId == workstream);
    }

    /// <inheritdoc />
    public async Task<UserAttribute> CreateAsync(UserAttribute userAttribute)
    {
        userAttribute.CreatedAt = DateTimeOffset.UtcNow;

        _context.UserAttributes.Add(userAttribute);
        await _context.SaveChangesAsync();

        return userAttribute;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UserAttribute userAttribute)
    {
        userAttribute.ModifiedAt = DateTimeOffset.UtcNow;

        _context.UserAttributes.Update(userAttribute);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var userAttribute = await _context.UserAttributes.FindAsync(id);
        if (userAttribute != null)
        {
            _context.UserAttributes.Remove(userAttribute);
            await _context.SaveChangesAsync();
        }
    }
}
