using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using Microsoft.Extensions.Logging;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Authorization.Roles;

/// <summary>
/// Service for managing Casbin roles with business logic.
/// Orchestrates repository calls and ViewModel mapping.
/// </summary>
public class RoleManagementService(
    IRoleRepository roleRepository,
    ILogger<RoleManagementService> logger) : IRoleManagementService
{
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly ILogger<RoleManagementService> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<CasbinRole>> GetRolesAsync(string workstream, string? search = null)
    {
        return await _roleRepository.SearchAsync(workstream, search);
    }

    /// <inheritdoc />
    public async Task<CasbinRole?> GetRoleByIdAsync(int id)
    {
        return await _roleRepository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<(bool Success, CasbinRole? Role, string? ErrorMessage)> CreateRoleAsync(
        RoleViewModel model, string workstream, string createdBy)
    {
        // Check for duplicate role names in the same workstream
        var isDuplicate = await _roleRepository.IsDuplicateRoleNameAsync(model.RoleName, workstream);
        if (isDuplicate)
        {
            return (false, null, $"A role with the name '{model.RoleName}' already exists in workstream '{workstream}'.");
        }

        var role = new CasbinRole
        {
            RoleName = model.RoleName,
            WorkstreamId = workstream,
            DisplayName = model.DisplayName,
            Description = model.Description,
            IsSystemRole = model.IsSystemRole,
            IsActive = model.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };

        var createdRole = await _roleRepository.CreateAsync(role);
        return (true, createdRole, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateRoleAsync(
        int id, RoleViewModel model, string modifiedBy)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return (false, "Role not found");
        }

        // Check for duplicate role names in the same workstream (excluding current role)
        var isDuplicate = await _roleRepository.IsDuplicateRoleNameAsync(
            model.RoleName, role.WorkstreamId, id);
        if (isDuplicate)
        {
            return (false, $"A role with the name '{model.RoleName}' already exists in workstream '{role.WorkstreamId}'.");
        }

        role.RoleName = model.RoleName;
        role.DisplayName = model.DisplayName;
        role.Description = model.Description;
        role.IsSystemRole = model.IsSystemRole;
        role.IsActive = model.IsActive;
        role.ModifiedAt = DateTimeOffset.UtcNow;
        role.ModifiedBy = modifiedBy;

        await _roleRepository.UpdateAsync(role);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> DeleteRoleAsync(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return (false, "Role not found");
        }

        // Check if role is a system role
        if (role.IsSystemRole)
        {
            return (false, "Cannot delete system roles.");
        }

        // Check if role is referenced in policies
        var isReferenced = await _roleRepository.IsReferencedInPoliciesAsync(role.RoleName);
        if (isReferenced)
        {
            return (false, $"Cannot delete role '{role.RoleName}' because it is referenced in policies. Remove those references first.");
        }

        await _roleRepository.DeleteAsync(id);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<bool> RoleExistsAsync(int id)
    {
        return await _roleRepository.ExistsAsync(id);
    }
}
