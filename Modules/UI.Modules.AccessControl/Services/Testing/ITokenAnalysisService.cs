using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Testing;

/// <summary>
/// Service for analyzing JWT tokens and extracting access control context.
/// </summary>
public interface ITokenAnalysisService
{
    /// <summary>
    /// Decodes a JWT token and analyzes the user's access control context.
    /// </summary>
    /// <param name="token">JWT access token</param>
    /// <param name="workstreamId">Workstream identifier</param>
    /// <returns>Token analysis result with claims, attributes, policies, and roles</returns>
    Task<TokenAnalysisResult> AnalyzeTokenAsync(string token, string workstreamId);
}
