using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository implementation for CasbinRole entity operations.
/// </summary>
public class CasbinRoleRepository(AccessControlDbContext context) : ICasbinRoleRepository
{
    private readonly AccessControlDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinRole>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CasbinRoles
            .Where(r => r.IsActive)
            .OrderBy(r => r.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinRole>> GetByWorkstreamAsync(string workstreamId, CancellationToken cancellationToken = default)
    {
        return await _context.CasbinRoles
            .Where(r => r.WorkstreamId == workstreamId && r.IsActive)
            .OrderBy(r => r.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CasbinRole?> GetByRoleNameAndWorkstreamAsync(string roleName, string workstreamId, CancellationToken cancellationToken = default)
    {
        return await _context.CasbinRoles
            .FirstOrDefaultAsync(r => r.RoleName == roleName && r.WorkstreamId == workstreamId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinRole>> SearchAsync(string? search, string? workstreamId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CasbinRoles.Where(r => r.IsActive);

        if (!string.IsNullOrWhiteSpace(workstreamId))
        {
            query = query.Where(r => r.WorkstreamId == workstreamId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(r =>
                r.DisplayName.ToLower().Contains(searchLower) ||
                r.RoleName.ToLower().Contains(searchLower));
        }

        return await query
            .OrderBy(r => r.DisplayName)
            .ToListAsync(cancellationToken);
    }
}
