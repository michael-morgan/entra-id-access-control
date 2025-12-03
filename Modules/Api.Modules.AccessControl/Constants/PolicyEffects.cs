namespace Api.Modules.AccessControl.Constants;

/// <summary>
/// Casbin policy effects.
/// </summary>
public static class PolicyEffects
{
    /// <summary>
    /// Allow access.
    /// </summary>
    public const string Allow = "allow";

    /// <summary>
    /// Deny access (overrides allow).
    /// </summary>
    public const string Deny = "deny";
}
