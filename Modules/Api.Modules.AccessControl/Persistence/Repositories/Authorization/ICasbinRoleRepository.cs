using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Persistence.Repositories.Authorization;

/// <summary>
/// Repository interface for CasbinRole entity operations.
/// </summary>
public interface ICasbinRoleRepository
{
    /// <summary>
    /// Get all roles.
    /// </summary>
    Task<IEnumerable<CasbinRole>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all roles for a specific workstream.
    /// </summary>
    Task<IEnumerable<CasbinRole>> GetByWorkstreamAsync(string workstreamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a role by its name and workstream.
    /// </summary>
    Task<CasbinRole?> GetByRoleNameAndWorkstreamAsync(string roleName, string workstreamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search roles by display name or role name.
    /// </summary>
    Task<IEnumerable<CasbinRole>> SearchAsync(string? search, string? workstreamId = null, CancellationToken cancellationToken = default);
}
