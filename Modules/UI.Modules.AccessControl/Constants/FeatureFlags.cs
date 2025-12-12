namespace UI.Modules.AccessControl.Constants;

/// <summary>
/// Feature flag constants for the AccessControl module.
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    /// Feature flag for Graph API functionality.
    /// When enabled, the application uses Microsoft Graph API to fetch user and group data.
    /// When disabled, the application uses local database tables for user data.
    /// </summary>
    public const string GraphApi = "GraphApi";
}
