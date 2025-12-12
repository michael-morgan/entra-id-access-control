namespace UI.Modules.AccessControl.Models;

/// <summary>
/// View model for displaying user information in the UI.
/// This model is agnostic to the data source (Graph API or local database).
/// </summary>
public class UserViewModel
{
    /// <summary>
    /// User ID from Entra ID (oid claim).
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Display name of the user.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// User's job title.
    /// Sourced from global UserAttributes or RoleAttributes (fallback).
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// User's department.
    /// Sourced from global UserAttributes or RoleAttributes (fallback).
    /// </summary>
    public string? Department { get; set; }
}
