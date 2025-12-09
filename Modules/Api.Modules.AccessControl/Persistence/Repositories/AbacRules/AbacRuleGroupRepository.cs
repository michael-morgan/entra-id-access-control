using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.AbacRules;

/// <summary>
/// Repository implementation for managing ABAC rule groups.
/// Provides data access for rule group CRUD operations with hierarchical support.
/// </summary>
public class AbacRuleGroupRepository(AccessControlDbContext context) : IAbacRuleGroupRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<AbacRuleGroup>> SearchAsync(string workstream, string? search = null)
    {
        var query = _context.AbacRuleGroups
            .Include(g => g.ParentGroup)
            .Include(g => g.ChildGroups)
            .Include(g => g.Rules)
            .AsSplitQuery() // Use split queries to avoid cartesian explosion
            .Where(g => g.WorkstreamId == workstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g => g.GroupName.Contains(search) ||
                                   (g.Description != null && g.Description.Contains(search)));
        }

        return await query
            .OrderBy(g => g.Priority)
            .ThenBy(g => g.GroupName)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AbacRuleGroup?> GetByIdAsync(int id)
    {
        return await _context.AbacRuleGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    /// <inheritdoc />
    public async Task<AbacRuleGroup?> GetByIdWithChildrenAsync(int id)
    {
        return await _context.AbacRuleGroups
            .Include(g => g.ParentGroup)
            .Include(g => g.ChildGroups)
            .Include(g => g.Rules)
            .AsSplitQuery() // Use split queries to avoid cartesian explosion
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    /// <inheritdoc />
    public async Task<AbacRuleGroup> CreateAsync(AbacRuleGroup ruleGroup)
    {
        ruleGroup.CreatedAt = DateTimeOffset.UtcNow;

        _context.AbacRuleGroups.Add(ruleGroup);
        await _context.SaveChangesAsync();

        return ruleGroup;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AbacRuleGroup ruleGroup)
    {
        ruleGroup.ModifiedAt = DateTimeOffset.UtcNow;

        _context.AbacRuleGroups.Update(ruleGroup);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var ruleGroup = await _context.AbacRuleGroups.FindAsync(id);
        if (ruleGroup != null)
        {
            _context.AbacRuleGroups.Remove(ruleGroup);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AbacRuleGroup>> GetParentOptionsAsync(string workstream, int? excludeId = null)
    {
        var query = _context.AbacRuleGroups
            .Where(g => g.WorkstreamId == workstream);

        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }

        return await query
            .OrderBy(g => g.GroupName)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> HasChildGroupsOrRulesAsync(int id)
    {
        var group = await _context.AbacRuleGroups
            .Include(g => g.ChildGroups)
            .Include(g => g.Rules)
            .AsSplitQuery() // Use split queries to avoid cartesian explosion
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);

        return group != null && (group.ChildGroups.Count > 0 || group.Rules.Count > 0);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.AbacRuleGroups.AnyAsync(g => g.Id == id);
    }
}
