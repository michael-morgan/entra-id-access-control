using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Correlation;

/// <summary>
/// Provides access to the current correlation context using AsyncLocal for async-safe flow.
/// </summary>
public class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<CorrelationContext?> _context = new();

    public CorrelationContext? Context
    {
        get => _context.Value;
        set => _context.Value = value;
    }
}
