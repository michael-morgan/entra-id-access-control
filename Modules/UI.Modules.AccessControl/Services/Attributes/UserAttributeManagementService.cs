using UI.Modules.AccessControl.Services.Graph;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Attributes;
using Api.Modules.AccessControl.Persistence.Repositories.AbacRules;
using Api.Modules.AccessControl.Persistence.Repositories.Audit;
using Microsoft.Extensions.Logging;
using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Attributes;

/// <summary>
/// Service for managing user attributes with business logic.
/// Orchestrates repository calls, Graph API lookups, and ViewModel mapping.
/// </summary>
public class UserAttributeManagementService(
    IUserAttributeRepository userAttributeRepository,
    IGraphUserService graphUserService,
    ILogger<UserAttributeManagementService> logger) : IUserAttributeManagementService
{
    private readonly IUserAttributeRepository _userAttributeRepository = userAttributeRepository;
    private readonly IGraphUserService _graphUserService = graphUserService;
    private readonly ILogger<UserAttributeManagementService> _logger = logger;

    /// <inheritdoc />
    public async Task<(IEnumerable<UserAttribute> UserAttributes, Dictionary<string, string> UserDisplayNames)>
        GetUserAttributesWithDisplayNamesAsync(string workstream, string? search = null)
    {
        var userAttributes = await _userAttributeRepository.SearchAsync(workstream, search);
        var userAttributesList = userAttributes.ToList();

        // Fetch display names from Graph API
        var displayNames = await FetchUserDisplayNamesAsync(userAttributesList.Select(ua => ua.UserId).ToList());

        return (userAttributesList, displayNames);
    }

    /// <inheritdoc />
    public async Task<(UserAttribute? UserAttribute, string? UserDisplayName)?> GetUserAttributeByIdWithDisplayNameAsync(int id)
    {
        var userAttribute = await _userAttributeRepository.GetByIdAsync(id);
        if (userAttribute == null)
        {
            return null;
        }

        var displayNames = await FetchUserDisplayNamesAsync([userAttribute.UserId]);
        var displayName = displayNames.TryGetValue(userAttribute.UserId, out var name) ? name : userAttribute.UserId;

        return (userAttribute, displayName);
    }

    /// <inheritdoc />
    public async Task<(bool Success, UserAttribute? UserAttribute, string? ErrorMessage)> CreateUserAttributeAsync(
        UserAttributeViewModel model, string workstream)
    {
        // Check if user already has attributes for this workstream
        var existing = await _userAttributeRepository.GetByUserIdAndWorkstreamAsync(model.UserId, workstream);
        if (existing != null)
        {
            return (false, null, "User attributes already exist for this user in this workstream.");
        }

        // Populate UserName from Graph API if not provided
        string? userName = model.UserName;
        if (string.IsNullOrEmpty(userName))
        {
            try
            {
                var users = await _graphUserService.GetUsersByIdsAsync([model.UserId]);
                if (users.TryGetValue(model.UserId, out var user))
                {
                    userName = user.DisplayName ?? model.UserId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch user display name for {UserId}", model.UserId);
            }
        }

        var userAttribute = new UserAttribute
        {
            UserId = model.UserId,
            WorkstreamId = workstream,
            UserName = userName,
            IsActive = model.IsActive,
            AttributesJson = model.AttributesJson,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _userAttributeRepository.CreateAsync(userAttribute);
        return (true, created, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateUserAttributeAsync(
        int id, UserAttributeViewModel model)
    {
        var userAttribute = await _userAttributeRepository.GetByIdAsync(id);
        if (userAttribute == null)
        {
            return (false, "User attribute not found");
        }

        userAttribute.IsActive = model.IsActive;
        userAttribute.AttributesJson = model.AttributesJson;
        userAttribute.ModifiedAt = DateTimeOffset.UtcNow;

        await _userAttributeRepository.UpdateAsync(userAttribute);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAttributeAsync(int id)
    {
        var userAttribute = await _userAttributeRepository.GetByIdAsync(id);
        if (userAttribute == null)
        {
            return false;
        }

        await _userAttributeRepository.DeleteAsync(id);
        return true;
    }

    /// <summary>
    /// Fetches user display names from Graph API.
    /// </summary>
    private async Task<Dictionary<string, string>> FetchUserDisplayNamesAsync(List<string> userIds)
    {
        var displayNames = new Dictionary<string, string>();

        if (userIds.Count == 0)
        {
            return displayNames;
        }

        try
        {
            var users = await _graphUserService.GetUsersByIdsAsync(userIds);
            foreach (var kvp in users)
            {
                displayNames[kvp.Key] = kvp.Value.DisplayName ?? kvp.Key;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch user display names from Graph API");
        }

        return displayNames;
    }
}
