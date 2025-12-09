namespace UI.Modules.AccessControl.Models;

public class ScenarioTestResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool IsAuthorized { get; set; }  // Overall authorization result
    public string Decision { get; set; } = string.Empty;
    public List<AuthorizationTestResult> TestResults { get; set; } = [];
    public List<AuthorizationStep> EvaluationTrace { get; set; } = [];  // Combined trace from all steps
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int TotalTests { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TestScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<TestCase> TestCases { get; set; } = [];
}

public class TestCase
{
    public string Description { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string WorkstreamId { get; set; } = string.Empty;
    public bool ExpectedResult { get; set; } // true = should be authorized, false = should be denied
    public string? MockEntityJson { get; set; }
}
