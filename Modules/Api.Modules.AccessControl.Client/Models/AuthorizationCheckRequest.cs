namespace Api.Modules.AccessControl.Client.Models;

/// <summary>
/// Request model for authorization check endpoint.
/// </summary>
public class AuthorizationCheckRequest
{
    /// <summary>
    /// The resource being accessed (e.g., "Loan/123", "Document/*").
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// The action being performed (e.g., "read", "write", "approve", "delete").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The workstream context for this authorization check.
    /// If not provided, uses X-Workstream-Id header or default from configuration.
    /// </summary>
    public string? WorkstreamId { get; set; }

    /// <summary>
    /// Optional entity data for ABAC evaluation (JSON object).
    /// Provides resource attributes for attribute-based authorization.
    /// Example: {"RequestedAmount": 150000, "Region": "US-WEST", "Status": "Submitted"}
    /// </summary>
    public object? EntityData { get; set; }
}
