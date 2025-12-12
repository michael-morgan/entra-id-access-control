using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository implementation for managing users.
/// Provides data access for user CRUD operations.
/// Users are global (not workstream-scoped) since they exist across all workstreams.
/// </summary>
public class UserRepository(AccessControlDbContext context) : IUserRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<User?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .OrderBy(u => u.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetByUserIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var userIdList = userIds.ToList();
        if (!userIdList.Any())
        {
            return Enumerable.Empty<User>();
        }

        return await _context.Users
            .Where(u => userIdList.Contains(u.UserId))
            .OrderBy(u => u.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => u.Name.Contains(searchTerm));
        }

        return await query
            .OrderBy(u => u.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        user.CreatedAt = DateTimeOffset.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.ModifiedAt = DateTimeOffset.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
