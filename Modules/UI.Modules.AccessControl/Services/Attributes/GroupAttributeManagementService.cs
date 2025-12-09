using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using UI.Modules.AccessControl.Models;
using UI.Modules.AccessControl.Services.Graph;

namespace UI.Modules.AccessControl.Services.Attributes;

/// <summary>
/// Service for managing group attributes with business logic.
/// Orchestrates repository calls, Graph API sync, and ViewModel mapping.
/// </summary>
public class GroupAttributeManagementService(
    IGroupAttributeRepository groupAttributeRepository,
    GraphGroupService graphGroupService,
    ILogger<GroupAttributeManagementService> logger) : IGroupAttributeManagementService
{
    private readonly IGroupAttributeRepository _groupAttributeRepository = groupAttributeRepository;
    private readonly GraphGroupService _graphGroupService = graphGroupService;
    private readonly ILogger<GroupAttributeManagementService> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<GroupAttribute>> GetGroupAttributesAsync(string workstream, string? search = null)
    {
        var groupAttributes = await _groupAttributeRepository.SearchAsync(workstream, search);
        var groupAttributesList = groupAttributes.ToList();

        // Sync group display names from Entra ID
        await SyncGroupDisplayNamesAsync(groupAttributesList);

        return groupAttributesList;
    }

    /// <inheritdoc />
    public async Task<GroupAttribute?> GetGroupAttributeByIdAsync(int id)
    {
        return await _groupAttributeRepository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<(bool Success, GroupAttribute? GroupAttribute, string? ErrorMessage)> CreateGroupAttributeAsync(
        GroupAttributeViewModel model, string workstream)
    {
        // Check if group already has attributes for this workstream
        var existing = await _groupAttributeRepository.GetByGroupIdAndWorkstreamAsync(model.GroupId, workstream);
        if (existing != null)
        {
            return (false, null, "Group attributes already exist for this group in this workstream.");
        }

        var groupAttribute = new GroupAttribute
        {
            GroupId = model.GroupId,
            WorkstreamId = workstream,
            GroupName = model.GroupName,
            IsActive = model.IsActive,
            AttributesJson = model.AttributesJson,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _groupAttributeRepository.CreateAsync(groupAttribute);
        return (true, created, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateGroupAttributeAsync(
        int id, GroupAttributeViewModel model)
    {
        var groupAttribute = await _groupAttributeRepository.GetByIdAsync(id);
        if (groupAttribute == null)
        {
            return (false, "Group attribute not found");
        }

        groupAttribute.GroupName = model.GroupName;
        groupAttribute.IsActive = model.IsActive;
        groupAttribute.AttributesJson = model.AttributesJson;
        groupAttribute.ModifiedAt = DateTimeOffset.UtcNow;

        await _groupAttributeRepository.UpdateAsync(groupAttribute);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteGroupAttributeAsync(int id)
    {
        var groupAttribute = await _groupAttributeRepository.GetByIdAsync(id);
        if (groupAttribute == null)
        {
            return false;
        }

        await _groupAttributeRepository.DeleteAsync(id);
        return true;
    }

    /// <summary>
    /// Syncs group display names from Entra ID Graph API.
    /// Updates the database if display names have changed.
    /// </summary>
    private async Task SyncGroupDisplayNamesAsync(List<GroupAttribute> groupAttributes)
    {
        if (groupAttributes.Count == 0)
        {
            return;
        }

        var groupIds = groupAttributes.Select(ga => ga.GroupId).Distinct().ToList();

        try
        {
            var groups = await _graphGroupService.GetGroupsByIdsAsync(groupIds);
            var updatedAttributes = new List<GroupAttribute>();

            foreach (var groupAttr in groupAttributes)
            {
                if (groups.TryGetValue(groupAttr.GroupId, out var group))
                {
                    var displayName = group.DisplayName ?? group.MailNickname ?? groupAttr.GroupId;
                    if (groupAttr.GroupName != displayName)
                    {
                        groupAttr.GroupName = displayName;
                        updatedAttributes.Add(groupAttr);
                    }
                }
            }

            if (updatedAttributes.Count > 0)
            {
                await _groupAttributeRepository.UpdateManyAsync(updatedAttributes);
                _logger.LogInformation("Updated {Count} group display names from Entra ID", updatedAttributes.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch group display names from Graph API");
        }
    }
}
