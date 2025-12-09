using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Testing;

/// <summary>
/// Service for testing authorization policies and ABAC rules.
/// </summary>
public interface IAuthorizationTestingService
{
    /// <summary>
    /// Tests whether a user (represented by a JWT token) is authorized for a specific action.
    /// </summary>
    /// <param name="request">Authorization test request with token, resource, and action</param>
    /// <returns>Authorization test result with decision and detailed evaluation steps</returns>
    Task<AuthorizationTestResult> CheckAuthorizationAsync(AuthorizationTestRequest request);
}
