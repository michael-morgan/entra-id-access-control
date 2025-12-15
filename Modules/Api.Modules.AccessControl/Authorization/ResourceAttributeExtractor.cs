using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Service responsible for extracting attributes from resource entities.
/// Uses reflection to extract all properties as a dictionary.
/// Handles both regular .NET objects and JSON deserialized objects (JsonElement).
/// </summary>
public class ResourceAttributeExtractor : IResourceAttributeExtractor
{
    /// <inheritdoc />
    public Dictionary<string, object> ExtractAttributes(object? resource)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (resource == null)
            return result;

        // Handle JsonElement (from JSON deserialization in API controllers)
        if (resource is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonElement.EnumerateObject())
                {
                    var value = UnwrapJsonElement(property.Value);
                    if (value != null)
                    {
                        result[property.Name] = value;
                    }
                }
            }
            return result;
        }

        // Handle regular .NET objects using reflection
        var properties = resource.GetType().GetProperties();
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(resource);
                if (value != null)
                {
                    result[property.Name] = value;
                }
            }
            catch (System.Reflection.TargetParameterCountException)
            {
                // Skip properties that require index parameters (like indexers)
                continue;
            }
        }

        return result;
    }

    /// <summary>
    /// Unwraps a JsonElement to its underlying primitive value.
    /// Handles strings, numbers (int/long/decimal/double), booleans, and null.
    /// For complex types (objects/arrays), returns the JsonElement itself for further processing.
    /// </summary>
    private static object? UnwrapJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? (object)intVal :
                                   element.TryGetInt64(out var longVal) ? (object)longVal :
                                   element.TryGetDecimal(out var decVal) ? (object)decVal :
                                   element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element, // Keep as JsonElement for array processing
            JsonValueKind.Object => element, // Keep as JsonElement for nested object processing
            _ => element.ToString()
        };
    }
}
