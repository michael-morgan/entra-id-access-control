namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Base class for all domain business events.
/// Inherit and add domain-specific properties.
/// </summary>
public abstract record BusinessEvent
{
    /// <summary>
    /// Event type derived from class name.
    /// Example: "LoanApplicationSubmitted"
    /// </summary>
    public string EventType => GetType().Name;

    /// <summary>
    /// Category for grouping and filtering.
    /// Override in derived classes.
    /// </summary>
    public abstract string EventCategory { get; }

    /// <summary>
    /// Schema version for event evolution.
    /// Increment when changing event structure.
    /// </summary>
    public virtual int EventVersion => 1;

    /// <summary>
    /// When the business action occurred.
    /// May differ from storage time for batch imports.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Entities affected by this event.
    /// Used for searching and correlation.
    /// </summary>
    public virtual IReadOnlyList<AffectedEntity> AffectedEntities =>
        Array.Empty<AffectedEntity>();
}

/// <summary>
/// Represents an entity affected by a business event.
/// </summary>
public record AffectedEntity(string EntityType, string EntityId)
{
    public override string ToString() => $"{EntityType}:{EntityId}";
}

/// <summary>
/// Marker interface for critical events that require synchronous write.
/// </summary>
public interface ICriticalEvent
{
}
