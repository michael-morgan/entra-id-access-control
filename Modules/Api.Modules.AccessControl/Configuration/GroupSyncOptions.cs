namespace Api.Modules.AccessControl.Configuration;

/// <summary>
/// Configuration options for JWT group synchronization.
/// Controls when and how user group memberships are persisted from JWT tokens.
/// </summary>
public class GroupSyncOptions
{
    /// <summary>
    /// Section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AccessControl:GroupSync";

    /// <summary>
    /// Enable or disable automatic JWT group synchronization.
    /// When enabled, group memberships are extracted from JWT tokens and persisted to database.
    /// Default: true.
    /// </summary>
    public bool EnableJwtGroupSync { get; set; } = true;

    /// <summary>
    /// Cache duration in minutes before re-syncing the same user's groups.
    /// Prevents redundant database writes on every request.
    /// Recommended: 15 minutes for balanced freshness and performance.
    /// Default: 15.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Number of days before a user-group association is considered "stale".
    /// Associations with LastSeenAt older than this threshold appear in stale reports.
    /// This is informational only - stale data doesn't affect authorization (JWT is source of truth).
    /// Default: 30.
    /// </summary>
    public int StaleThresholdDays { get; set; } = 30;
}
