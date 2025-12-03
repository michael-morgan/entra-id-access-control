using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Provides access to the current correlation context.
/// Uses AsyncLocal for async-safe ambient context.
/// </summary>
public interface ICorrelationContextAccessor
{
    /// <summary>
    /// Gets or sets the correlation context for the current request.
    /// </summary>
    CorrelationContext? Context { get; set; }
}
