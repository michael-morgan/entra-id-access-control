using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository implementation for Group entity operations.
/// </summary>
public class GroupRepository(AccessControlDbContext context) : IGroupRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<Group?> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .FirstOrDefaultAsync(g => g.GroupId == groupId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Group>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .OrderBy(g => g.DisplayName ?? g.GroupId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Group>> GetByGroupIdsAsync(List<string> groupIds, CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .Where(g => groupIds.Contains(g.GroupId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync(cancellationToken);
        return group;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        group.ModifiedAt = DateTimeOffset.UtcNow;
        _context.Groups.Update(group);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Group>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Groups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(g =>
                (g.DisplayName != null && g.DisplayName.ToLower().Contains(searchLower)) ||
                g.GroupId.ToLower().Contains(searchLower));
        }

        return await query
            .OrderBy(g => g.DisplayName ?? g.GroupId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Group> GetOrCreateAsync(string groupId, string? displayName, string source, CancellationToken cancellationToken = default)
    {
        // Try to find existing group
        var existingGroup = await GetByGroupIdAsync(groupId, cancellationToken);
        if (existingGroup != null)
        {
            return existingGroup;
        }

        // Create new group
        var newGroup = new Group
        {
            GroupId = groupId,
            DisplayName = displayName ?? groupId, // Default to OID if no friendly name
            Source = source,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await AddAsync(newGroup, cancellationToken);
    }
}
