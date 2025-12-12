using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Microsoft.EntityFrameworkCore;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Groups;

/// <summary>
/// Service implementation for managing groups in the admin UI.
/// </summary>
public class GroupManagementService(
    IGroupRepository groupRepository,
    IUserGroupRepository userGroupRepository) : IGroupManagementService
{
    private readonly IGroupRepository _groupRepository = groupRepository;
    private readonly IUserGroupRepository _userGroupRepository = userGroupRepository;

    /// <inheritdoc />
    public async Task<List<GroupListItemViewModel>> GetAllGroupsAsync()
    {
        var groups = await _groupRepository.GetAllAsync();
        var groupViewModels = new List<GroupListItemViewModel>();

        foreach (var group in groups)
        {
            var userCount = await GetUserCountAsync(group.GroupId);

            groupViewModels.Add(new GroupListItemViewModel
            {
                GroupId = group.GroupId,
                DisplayName = group.DisplayName ?? group.GroupId,
                Description = group.Description,
                Source = group.Source,
                CreatedAt = group.CreatedAt,
                UserCount = userCount
            });
        }

        return groupViewModels
            .OrderBy(g => g.NeedsEnrichment ? 0 : 1) // Show groups needing enrichment first
            .ThenBy(g => g.DisplayName)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<GroupDetailsViewModel?> GetGroupDetailsAsync(string groupId)
    {
        var group = await _groupRepository.GetByGroupIdAsync(groupId);
        if (group == null)
        {
            return null;
        }

        var members = await _userGroupRepository.GetGroupUsersAsync(groupId);

        return new GroupDetailsViewModel
        {
            GroupId = group.GroupId,
            DisplayName = group.DisplayName ?? group.GroupId,
            Description = group.Description,
            Source = group.Source,
            CreatedAt = group.CreatedAt,
            Members = members
        };
    }

    /// <inheritdoc />
    public async Task<bool> UpdateGroupAsync(string groupId, string displayName, string? description)
    {
        var group = await _groupRepository.GetByGroupIdAsync(groupId);
        if (group == null)
        {
            return false;
        }

        group.DisplayName = displayName;
        group.Description = description;

        await _groupRepository.UpdateAsync(group);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<StaleAssociationViewModel>> GetStaleAssociationsAsync(int daysThreshold)
    {
        var staleAssociations = await _userGroupRepository.GetStaleAssociationsAsync(daysThreshold);
        var viewModels = new List<StaleAssociationViewModel>();

        foreach (var association in staleAssociations)
        {
            var daysSinceLastSeen = (DateTimeOffset.UtcNow - association.LastSeenAt).Days;

            viewModels.Add(new StaleAssociationViewModel
            {
                Id = association.Id,
                UserId = association.UserId,
                UserName = association.User?.Name ?? association.UserId,
                GroupId = association.GroupId,
                GroupDisplayName = association.Group?.DisplayName ?? association.GroupId,
                FirstSeenAt = association.FirstSeenAt,
                LastSeenAt = association.LastSeenAt,
                Source = association.Source,
                DaysSinceLastSeen = daysSinceLastSeen
            });
        }

        return viewModels
            .OrderByDescending(v => v.DaysSinceLastSeen)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<bool> KeepStaleAssociationAsync(int associationId)
    {
        var association = await _userGroupRepository.GetByIdAsync(associationId);
        if (association == null)
        {
            return false;
        }

        // Convert source from JWT to Manual (user wants to keep this association)
        association.Source = "Manual";
        await _userGroupRepository.UpsertUserGroupAsync(association.UserId, association.GroupId, "Manual");
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveStaleAssociationAsync(int associationId)
    {
        var association = await _userGroupRepository.GetByIdAsync(associationId);
        if (association == null)
        {
            return false;
        }

        await _userGroupRepository.DeleteAsync(associationId);
        return true;
    }

    private async Task<int> GetUserCountAsync(string groupId)
    {
        var members = await _userGroupRepository.GetGroupUsersAsync(groupId);
        return members.Count;
    }
}
