using System.Security.Claims;
using Api.Modules.AccessControl.Configuration;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Api.Modules.AccessControl.Persistence.Repositories.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Services;

/// <summary>
/// Service for synchronizing user group memberships from JWT tokens to the database.
/// Implements caching to prevent redundant database writes on every request.
/// </summary>
public class JwtGroupSyncService(
    IGroupRepository groupRepository,
    IUserGroupRepository userGroupRepository,
    IUserRepository userRepository,
    IMemoryCache memoryCache,
    IOptions<GroupSyncOptions> options,
    ILogger<JwtGroupSyncService> logger) : IJwtGroupSyncService
{
    private readonly IGroupRepository _groupRepository = groupRepository;
    private readonly IUserGroupRepository _userGroupRepository = userGroupRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly GroupSyncOptions _options = options.Value;
    private readonly ILogger<JwtGroupSyncService> _logger = logger;

    /// <inheritdoc />
    public async Task SyncUserGroupsFromJwtAsync(ClaimsPrincipal user)
    {
        try
        {
            // Extract user ID from OID claim
            var userId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? user.FindFirst("oid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogDebug("No user ID (oid) found in JWT claims, skipping group sync");
                return;
            }

            // Check cache to avoid redundant syncs
            var cacheKey = $"GroupSync:{userId}";
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                _logger.LogDebug("User {UserId} group sync cached, skipping", userId);
                return;
            }

            // Extract group OIDs from JWT
            var groupClaims = user.FindAll("groups").ToList();
            if (groupClaims.Count == 0)
            {
                _logger.LogDebug("No groups found in JWT for user {UserId}", userId);
                // Still cache to avoid repeated checks
                _memoryCache.Set(cacheKey, true, TimeSpan.FromMinutes(_options.CacheDurationMinutes));
                return;
            }

            var groupIds = groupClaims.Select(c => c.Value).ToList();
            _logger.LogInformation("Syncing {GroupCount} groups for user {UserId}", groupIds.Count, userId);

            // Ensure user exists in database (create if needed)
            var existingUser = await _userRepository.GetByUserIdAsync(userId);
            if (existingUser == null)
            {
                var userName = user.FindFirst("name")?.Value ?? userId;
                var newUser = new User
                {
                    UserId = userId,
                    Name = userName,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _userRepository.AddAsync(newUser);
                _logger.LogInformation("Created user record for {UserId} ({Name})", userId, userName);
            }

            // Process each group
            foreach (var groupId in groupIds)
            {
                try
                {
                    // Get or create group record (defaults DisplayName to OID if new)
                    var group = await _groupRepository.GetOrCreateAsync(
                        groupId: groupId,
                        displayName: null, // Will default to groupId (OID)
                        source: GroupSource.JWT);

                    // Upsert user-group association (updates LastSeenAt if exists)
                    await _userGroupRepository.UpsertUserGroupAsync(
                        userId: userId,
                        groupId: groupId,
                        source: UserGroupSource.JWT);

                    _logger.LogDebug("Synced group {GroupId} for user {UserId}", groupId, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing group {GroupId} for user {UserId}", groupId, userId);
                    // Continue processing other groups
                }
            }

            // Cache successful sync
            _memoryCache.Set(cacheKey, true, TimeSpan.FromMinutes(_options.CacheDurationMinutes));
            _logger.LogInformation("Successfully synced {GroupCount} groups for user {UserId}, cached for {Minutes} minutes",
                groupIds.Count, userId, _options.CacheDurationMinutes);
        }
        catch (Exception ex)
        {
            // Log but don't throw - this is a background operation that shouldn't block requests
            _logger.LogError(ex, "Fatal error during JWT group sync");
        }
    }
}
