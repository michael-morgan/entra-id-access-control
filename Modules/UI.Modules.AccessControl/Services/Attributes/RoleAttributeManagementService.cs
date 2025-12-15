using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;

namespace UI.Modules.AccessControl.Services.Attributes;

public class RoleAttributeManagementService(
    IRoleAttributeRepository roleAttributeRepository,
    ICasbinRoleRepository casbinRoleRepository) : IRoleAttributeManagementService
{
    private readonly IRoleAttributeRepository _roleAttributeRepository = roleAttributeRepository;
    private readonly ICasbinRoleRepository _casbinRoleRepository = casbinRoleRepository;

    public async Task<IEnumerable<RoleAttribute>> GetRoleAttributesAsync(string workstream, string? search = null)
    {
        return await _roleAttributeRepository.SearchAsync(workstream, search);
    }

    public async Task<RoleAttribute?> GetRoleAttributeByIdAsync(int id)
    {
        return await _roleAttributeRepository.GetByIdAsync(id);
    }

    public async Task<(bool Success, RoleAttribute? RoleAttribute, string? ErrorMessage)> CreateRoleAttributeAsync(RoleAttribute roleAttribute, string workstream)
    {
        // Check for duplicates
        var existing = await _roleAttributeRepository.GetByRoleAndWorkstreamAsync(
            roleAttribute.RoleId, workstream);

        if (existing != null)
        {
            return (false, null, "Role attributes already exist for this role in this workstream.");
        }

        // If RoleName is not set, populate it from CasbinRoles
        if (string.IsNullOrEmpty(roleAttribute.RoleName))
        {
            var casbinRole = await _casbinRoleRepository.GetByRoleNameAndWorkstreamAsync(roleAttribute.RoleId, workstream);
            roleAttribute.RoleName = casbinRole?.DisplayName;
        }

        roleAttribute.WorkstreamId = workstream;
        var created = await _roleAttributeRepository.CreateAsync(roleAttribute);
        return (true, created, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateRoleAttributeAsync(int id, RoleAttribute roleAttribute)
    {
        var existing = await _roleAttributeRepository.GetByIdAsync(id);
        if (existing == null) return (false, "Role attribute not found");

        await _roleAttributeRepository.UpdateAsync(roleAttribute);
        return (true, null);
    }

    public async Task<bool> DeleteRoleAttributeAsync(int id)
    {
        var roleAttribute = await _roleAttributeRepository.GetByIdAsync(id);
        if (roleAttribute == null) return false;

        await _roleAttributeRepository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> RoleAttributeExistsForWorkstreamAsync(string roleId, string workstream, int? excludeId = null)
    {
        var existing = await _roleAttributeRepository.GetByRoleAndWorkstreamAsync(roleId, workstream);
        return existing != null && (!excludeId.HasValue || existing.Id != excludeId.Value);
    }

    public async Task<IEnumerable<CasbinRole>> GetRolesForWorkstreamAsync(string workstream)
    {
        return await _casbinRoleRepository.GetByWorkstreamAsync(workstream);
    }
}
