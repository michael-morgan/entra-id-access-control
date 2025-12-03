namespace Api.Modules.AccessControl.Constants;

/// <summary>
/// Custom claim type names for authorization.
/// </summary>
public static class ClaimTypes
{
    /// <summary>
    /// Object ID from Entra ID (user's unique identifier).
    /// </summary>
    public const string ObjectId = "oid";

    /// <summary>
    /// User's email address.
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// User's display name.
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// App roles assigned in Entra ID.
    /// </summary>
    public const string Roles = "roles";

    /// <summary>
    /// Security group memberships.
    /// </summary>
    public const string Groups = "groups";

    /// <summary>
    /// Custom claim: User's department.
    /// </summary>
    public const string Department = "department";

    /// <summary>
    /// Custom claim: User's region/territory.
    /// </summary>
    public const string Region = "region";

    /// <summary>
    /// Custom claim: Maximum approval limit.
    /// </summary>
    public const string ApprovalLimit = "approval_limit";

    /// <summary>
    /// Custom claim: Management level (hierarchy).
    /// </summary>
    public const string ManagementLevel = "management_level";

    /// <summary>
    /// Custom claim: Workstream assignments.
    /// </summary>
    public const string Workstreams = "workstreams";
}
