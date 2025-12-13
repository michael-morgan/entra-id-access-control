namespace Api.Modules.AccessControl.Client.Http;

/// <summary>
/// Interface for providing access tokens to the AccessControl client.
/// Implement this interface in your external app to provide JWT tokens.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Gets an access token for calling the AccessControl API.
    /// </summary>
    /// <returns>JWT access token, or null if not available</returns>
    Task<string?> GetAccessTokenAsync();
}