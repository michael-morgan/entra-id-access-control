namespace Api.Modules.AccessControl.Models;

/// <summary>
/// Marker interface for entities that are scoped to a workstream.
/// Used by EF Core global query filters.
/// </summary>
public interface IWorkstreamScoped
{
    string WorkstreamId { get; }
}

/// <summary>
/// Marker interface for entities that are scoped to a region.
/// Used by EF Core global query filters.
/// </summary>
public interface IRegionScoped
{
    string Region { get; }
}

/// <summary>
/// Marker interface for entities that can be identified.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}
