namespace UI.Modules.AccessControl.Models;

public class AuthorizationTestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public string TestDescription { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string WorkstreamId { get; set; } = string.Empty;

    public bool IsAuthorized { get; set; }
    public string Decision { get; set; } = string.Empty; // "Allowed" or "Denied"

    public List<AuthorizationStep> EvaluationTrace { get; set; } = [];

    public string? MockEntityJson { get; set; }

    /// <summary>
    /// Formatted diagnostic message explaining why the test failed (for display in UI).
    /// Includes RBAC and ABAC diagnostics with actionable suggestions.
    /// </summary>
    public string? DiagnosticMessage { get; set; }
}

public class AuthorizationStep
{
    public string StepName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty; // "Pass", "Fail", "Skip"
    public string? Details { get; set; }
    public int Order { get; set; }
}
