namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Represents the current authenticated user.
/// </summary>
public sealed record CurrentUser
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Email { get; init; }
    public required UserType Type { get; init; }
    public string? IpAddress { get; init; }
    public required string WorkstreamId { get; init; }
}

/// <summary>
/// Type of actor performing the action.
/// </summary>
public enum UserType
{
    User,
    System,
    Service
}
