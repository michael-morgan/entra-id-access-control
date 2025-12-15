using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace UI.Modules.AccessControl.Services.Attributes;

public interface IRoleAttributeManagementService
{
    Task<IEnumerable<RoleAttribute>> GetRoleAttributesAsync(string workstream, string? search = null);
    Task<RoleAttribute?> GetRoleAttributeByIdAsync(int id);
    Task<(bool Success, RoleAttribute? RoleAttribute, string? ErrorMessage)> CreateRoleAttributeAsync(RoleAttribute roleAttribute, string workstream);
    Task<(bool Success, string? ErrorMessage)> UpdateRoleAttributeAsync(int id, RoleAttribute roleAttribute);
    Task<bool> DeleteRoleAttributeAsync(int id);
    Task<bool> RoleAttributeExistsForWorkstreamAsync(string roleId, string workstream, int? excludeId = null);
    Task<IEnumerable<CasbinRole>> GetRolesForWorkstreamAsync(string workstream);
}
