using Api.Modules.AccessControl.Interfaces;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Service responsible for extracting attributes from resource entities.
/// Uses reflection to extract all properties as a dictionary.
/// </summary>
public class ResourceAttributeExtractor : IResourceAttributeExtractor
{
    /// <inheritdoc />
    public Dictionary<string, object> ExtractAttributes(object? resource)
    {
        var result = new Dictionary<string, object>();

        if (resource == null)
            return result;

        var properties = resource.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(resource);
            if (value != null)
            {
                result[property.Name] = value;
            }
        }

        return result;
    }
}
