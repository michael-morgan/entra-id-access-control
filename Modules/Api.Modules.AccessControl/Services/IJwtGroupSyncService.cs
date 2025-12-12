using System.Security.Claims;

namespace Api.Modules.AccessControl.Services;

/// <summary>
/// Service for synchronizing user group memberships from JWT tokens to the database.
/// This enables display of group information in the admin UI when Graph API is disabled.
/// </summary>
/// <remarks>
/// IMPORTANT: This service is for UI display purposes only.
/// Authorization decisions ALWAYS use the JWT groups claim directly (source of truth).
/// Database groups are enriched with friendly names for admin UI convenience.
/// </remarks>
public interface IJwtGroupSyncService
{
    /// <summary>
    /// Synchronizes a user's group memberships from their JWT token to the database.
    /// Extracts groups claim, creates/updates Groups records, and upserts UserGroups associations.
    /// Uses in-memory caching to prevent redundant database writes.
    /// </summary>
    /// <param name="user">ClaimsPrincipal from authenticated request (contains JWT claims)</param>
    /// <returns>Task representing the async operation</returns>
    /// <remarks>
    /// This method is designed to be fire-and-forget from middleware.
    /// Errors are logged but not thrown to avoid blocking the request pipeline.
    /// Cache prevents DB writes for the same user within the configured time window.
    /// </remarks>
    Task SyncUserGroupsFromJwtAsync(ClaimsPrincipal user);
}
