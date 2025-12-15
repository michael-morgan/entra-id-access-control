using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository implementation for managing Casbin policies.
/// Provides data access for policy CRUD operations.
/// </summary>
public class PolicyRepository(AccessControlDbContext context) : IPolicyRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinPolicy>> SearchAsync(
        string workstream,
        string? policyType = null,
        string? search = null)
    {
        var query = _context.CasbinPolicies
            .Where(p => p.WorkstreamId == workstream || p.WorkstreamId == null);

        if (!string.IsNullOrWhiteSpace(policyType))
        {
            query = query.Where(p => p.PolicyType == policyType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.V0.Contains(search) ||
                (p.V1 != null && p.V1.Contains(search)) ||
                (p.V2 != null && p.V2.Contains(search)));
        }

        return await query
            .OrderBy(p => p.PolicyType)
            .ThenBy(p => p.V0)
            .ThenBy(p => p.V1)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CasbinPolicy?> GetByIdAsync(int id)
    {
        return await _context.CasbinPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <inheritdoc />
    public async Task<CasbinPolicy> CreateAsync(CasbinPolicy policy)
    {
        policy.CreatedAt = DateTimeOffset.UtcNow;
        policy.ModifiedAt = DateTimeOffset.UtcNow;

        _context.CasbinPolicies.Add(policy);
        await _context.SaveChangesAsync();

        return policy;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CasbinPolicy policy)
    {
        policy.ModifiedAt = DateTimeOffset.UtcNow;

        _context.CasbinPolicies.Update(policy);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var policy = await _context.CasbinPolicies.FindAsync(id);
        if (policy != null)
        {
            _context.CasbinPolicies.Remove(policy);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.CasbinPolicies
            .AsNoTracking()
            .AnyAsync(p => p.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetPolicyTypesAsync(string workstream)
    {
        return await _context.CasbinPolicies
            .Where(p => p.WorkstreamId == workstream || p.WorkstreamId == null)
            .Select(p => p.PolicyType)
            .Distinct()
            .OrderBy(pt => pt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetWorkstreamsAsync()
    {
        return await _context.CasbinPolicies
            .Where(p => p.WorkstreamId != null)
            .Select(p => p.WorkstreamId!)
            .Distinct()
            .OrderBy(w => w)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinPolicy>> GetBySubjectIdsAsync(IEnumerable<string> subjectIds, string? policyType = null)
    {
        var subjectIdsList = subjectIds.ToList();

        var query = _context.CasbinPolicies
            .Where(p => p.IsActive && subjectIdsList.Contains(p.V0!));

        if (!string.IsNullOrWhiteSpace(policyType))
        {
            query = query.Where(p => p.PolicyType == policyType);
        }

        return await query
            .OrderBy(p => p.WorkstreamId)
            .ThenBy(p => p.PolicyType)
            .ThenBy(p => p.V0)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinPolicy>> GetRolesForSubjectsAsync(
        IEnumerable<string> subjectIds,
        string workstream,
        CancellationToken cancellationToken = default)
    {
        var subjectIdsList = subjectIds.ToList();

        return await _context.CasbinPolicies
            .Where(p => p.IsActive)
            .Where(p => p.PolicyType == "g")
            .Where(p => p.V2 == workstream)
            .Where(p => subjectIdsList.Contains(p.V0!))
            .OrderBy(p => p.V0)
            .ThenBy(p => p.V1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
