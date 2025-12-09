using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.AbacRules;

/// <summary>
/// Repository implementation for managing ABAC rules.
/// Provides data access for rule CRUD operations.
/// </summary>
public class AbacRuleRepository(AccessControlDbContext context) : IAbacRuleRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<AbacRule>> SearchAsync(string workstream, string? search = null, string? ruleType = null, int? ruleGroupId = null)
    {
        var query = _context.AbacRules
            .Include(r => r.RuleGroup)
            .Where(r => r.WorkstreamId == workstream);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.RuleName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(ruleType))
        {
            query = query.Where(r => r.RuleType == ruleType);
        }

        if (ruleGroupId.HasValue)
        {
            query = query.Where(r => r.RuleGroupId == ruleGroupId.Value);
        }

        return await query
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.RuleName)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AbacRule?> GetByIdAsync(int id)
    {
        return await _context.AbacRules
            .Include(r => r.RuleGroup)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<AbacRule> CreateAsync(AbacRule rule)
    {
        rule.CreatedAt = DateTimeOffset.UtcNow;

        _context.AbacRules.Add(rule);
        await _context.SaveChangesAsync();

        return rule;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AbacRule rule)
    {
        rule.ModifiedAt = DateTimeOffset.UtcNow;

        _context.AbacRules.Update(rule);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var rule = await _context.AbacRules.FindAsync(id);
        if (rule != null)
        {
            _context.AbacRules.Remove(rule);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.AbacRules.AnyAsync(r => r.Id == id);
    }
}
