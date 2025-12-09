using UI.Modules.AccessControl.Models;

namespace UI.Modules.AccessControl.Services.Testing;

/// <summary>
/// Service for running predefined authorization test scenarios.
/// </summary>
public interface IScenarioTestingService
{
    /// <summary>
    /// Gets available test scenarios for a workstream.
    /// </summary>
    /// <param name="token">JWT access token</param>
    /// <param name="workstreamId">Workstream identifier</param>
    /// <returns>List of available test scenarios</returns>
    Task<List<DynamicScenario>> GetAvailableScenariosAsync(string token, string workstreamId);

    /// <summary>
    /// Runs a specific test scenario and returns results for all test cases.
    /// </summary>
    /// <param name="scenarioName">Name of the scenario to run</param>
    /// <param name="token">JWT access token</param>
    /// <param name="workstreamId">Workstream identifier</param>
    /// <returns>Scenario test result with individual test case results</returns>
    Task<ScenarioTestResult> RunScenarioAsync(string scenarioName, string token, string workstreamId);
}

/// <summary>
/// Represents a dynamically generated scenario based on available resources.
/// </summary>
public class DynamicScenario
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Resource { get; init; }
    public required List<string> AvailableActions { get; init; }
    public required string WorkstreamId { get; init; }
}
