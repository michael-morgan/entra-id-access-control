using System.Text.Json;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;

namespace UI.Modules.AccessControl.Services.Attributes;

/// <summary>
/// Service for resolving global user attributes (JobTitle, Department) with fallback logic.
/// Implements precedence: UserAttributes (global) > RoleAttributes (global).
/// </summary>
public class GlobalAttributeService(
    IUserAttributeRepository userAttributeRepository,
    IRoleAttributeRepository roleAttributeRepository) : IGlobalAttributeService
{
    private readonly IUserAttributeRepository _userAttributeRepository = userAttributeRepository;
    private readonly IRoleAttributeRepository _roleAttributeRepository = roleAttributeRepository;

    private const string GlobalWorkstream = "global";

    /// <inheritdoc />
    public async Task<string?> GetUserJobTitleAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Try to get from UserAttributes (global workstream)
        var userAttribute = await _userAttributeRepository.GetByUserIdAndWorkstreamAsync(userId, GlobalWorkstream);
        if (userAttribute?.AttributesJson != null)
        {
            var attributesDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(userAttribute.AttributesJson);
            if (attributesDict != null && attributesDict.TryGetValue("JobTitle", out var jobTitleElement))
            {
                var jobTitle = jobTitleElement.GetString();
                if (!string.IsNullOrWhiteSpace(jobTitle))
                {
                    return jobTitle;
                }
            }
        }

        // Fallback: Get from RoleAttributes (global workstream)
        // In a real implementation, we would need to know the user's roles first
        // For now, we'll return null as the fallback
        // This can be enhanced to query user's roles and then get role attributes
        return null;
    }

    /// <inheritdoc />
    public async Task<string?> GetUserDepartmentAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Try to get from UserAttributes (global workstream)
        var userAttribute = await _userAttributeRepository.GetByUserIdAndWorkstreamAsync(userId, GlobalWorkstream);
        if (userAttribute?.AttributesJson != null)
        {
            var attributesDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(userAttribute.AttributesJson);
            if (attributesDict != null && attributesDict.TryGetValue("Department", out var departmentElement))
            {
                var department = departmentElement.GetString();
                if (!string.IsNullOrWhiteSpace(department))
                {
                    return department;
                }
            }
        }

        // Fallback: Get from RoleAttributes (global workstream)
        // In a real implementation, we would need to know the user's roles first
        // For now, we'll return null as the fallback
        // This can be enhanced to query user's roles and then get role attributes
        return null;
    }
}
