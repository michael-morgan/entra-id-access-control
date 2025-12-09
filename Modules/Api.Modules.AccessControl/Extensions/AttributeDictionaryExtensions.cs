using System.Text.Json;

namespace Api.Modules.AccessControl.Extensions;

/// <summary>
/// Extension methods for type-safe access to dynamic attribute dictionaries.
/// Used by ABAC system to access user/resource attributes stored as JSON in the database.
/// </summary>
public static class AttributeDictionaryExtensions
{
    /// <summary>
    /// Gets a typed value from the dictionary with automatic type conversion.
    /// Handles JsonElement conversion and basic type coercion.
    /// </summary>
    public static T? Get<T>(this Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
            return default;

        if (value is T typedValue)
            return typedValue;

        if (value is JsonElement jsonElement)
            return ConvertJsonElement<T>(jsonElement);

        // Attempt type conversion for common scenarios
        try
        {
            // Handle nullable types
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (value == null)
                return default;

            var converted = Convert.ChangeType(value, targetType);
            return (T)converted;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Converts a JsonElement to the target type using JSON deserialization.
    /// </summary>
    private static T? ConvertJsonElement<T>(JsonElement element)
    {
        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => default,
                JsonValueKind.String when typeof(T) == typeof(string) => (T)(object)element.GetString()!,
                JsonValueKind.Number when typeof(T) == typeof(int) || typeof(T) == typeof(int?) => (T)(object)element.GetInt32(),
                JsonValueKind.Number when typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?) => (T)(object)element.GetDecimal(),
                JsonValueKind.Number when typeof(T) == typeof(double) || typeof(T) == typeof(double?) => (T)(object)element.GetDouble(),
                JsonValueKind.True or JsonValueKind.False when typeof(T) == typeof(bool) || typeof(T) == typeof(bool?) => (T)(object)element.GetBoolean(),
                _ => JsonSerializer.Deserialize<T>(element.GetRawText())
            };
        }
        catch
        {
            return default;
        }
    }

    // Convenience methods for common types

    public static string? GetString(this Dictionary<string, object> dict, string key)
        => Get<string>(dict, key);

    public static decimal? GetDecimal(this Dictionary<string, object> dict, string key)
        => Get<decimal?>(dict, key);

    public static int? GetInt(this Dictionary<string, object> dict, string key)
        => Get<int?>(dict, key);

    public static bool? GetBool(this Dictionary<string, object> dict, string key)
        => Get<bool?>(dict, key);

    public static DateTimeOffset? GetDateTimeOffset(this Dictionary<string, object> dict, string key)
        => Get<DateTimeOffset?>(dict, key);

    public static DateTime? GetDateTime(this Dictionary<string, object> dict, string key)
        => Get<DateTime?>(dict, key);
}
