using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository implementation for UserGroup entity operations.
/// </summary>
public class UserGroupRepository(AccessControlDbContext context) : IUserGroupRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<List<UserGroup>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGroups
            .Include(ug => ug.Group)
            .Include(ug => ug.User)
            .Where(ug => ug.UserId == userId)
            .OrderBy(ug => ug.Group!.DisplayName ?? ug.Group!.GroupId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetGroupIdsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUserIdsByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGroups
            .Where(ug => ug.GroupId == groupId)
            .Select(ug => ug.UserId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<UserGroup>> GetGroupUsersAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return await _context.UserGroups
            .Include(ug => ug.User)
            .Include(ug => ug.Group)
            .Where(ug => ug.GroupId == groupId)
            .OrderBy(ug => ug.User!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserGroup> UpsertUserGroupAsync(string userId, string groupId, string source, CancellationToken cancellationToken = default)
    {
        // Try to find existing association
        var existing = await _context.UserGroups
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId, cancellationToken);

        if (existing != null)
        {
            // Update LastSeenAt timestamp
            existing.LastSeenAt = DateTimeOffset.UtcNow;
            _context.UserGroups.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        // Create new association
        var newUserGroup = new UserGroup
        {
            UserId = userId,
            GroupId = groupId,
            Source = source,
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };

        _context.UserGroups.Add(newUserGroup);
        await _context.SaveChangesAsync(cancellationToken);
        return newUserGroup;
    }

    /// <inheritdoc />
    public async Task<List<UserGroup>> GetStaleAssociationsAsync(int daysThreshold, CancellationToken cancellationToken = default)
    {
        var thresholdDate = DateTimeOffset.UtcNow.AddDays(-daysThreshold);

        return await _context.UserGroups
            .Include(ug => ug.User)
            .Include(ug => ug.Group)
            .Where(ug => ug.LastSeenAt < thresholdDate)
            .OrderBy(ug => ug.LastSeenAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var userGroup = await _context.UserGroups.FindAsync([id], cancellationToken);
        if (userGroup != null)
        {
            _context.UserGroups.Remove(userGroup);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<UserGroup?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.UserGroups
            .Include(ug => ug.User)
            .Include(ug => ug.Group)
            .FirstOrDefaultAsync(ug => ug.Id == id, cancellationToken);
    }
}
