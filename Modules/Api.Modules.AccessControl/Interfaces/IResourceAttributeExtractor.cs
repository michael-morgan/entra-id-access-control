namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Service responsible for extracting attributes from resource entities.
/// Uses reflection to extract all properties as a dictionary.
/// </summary>
public interface IResourceAttributeExtractor
{
    /// <summary>
    /// Extracts all properties from a resource entity as a dictionary.
    /// Returns empty dictionary if resource is null.
    /// </summary>
    /// <param name="resource">The resource entity to extract attributes from</param>
    /// <returns>Dictionary of property names to values</returns>
    Dictionary<string, object> ExtractAttributes(object? resource);
}
