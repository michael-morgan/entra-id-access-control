using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Service responsible for merging attributes from multiple sources.
/// Implements precedence rule: User > Role > Group.
/// </summary>
public class AttributeMerger : IAttributeMerger
{
    /// <inheritdoc />
    public Dictionary<string, object> MergeAttributes(
        IDictionary<string, GroupAttributes> groupAttributes,
        IDictionary<string, RoleAttributes> roleAttributes,
        UserAttributes? userAttributes)
    {
        var result = new Dictionary<string, object>();

        // 1. Start with group attributes (lowest precedence)
        foreach (var group in groupAttributes.Values)
        {
            foreach (var attr in group.Attributes)
            {
                if (!result.ContainsKey(attr.Key))
                {
                    result[attr.Key] = ConvertJsonElement(attr.Value);
                }
            }
        }

        // 2. Override with role attributes (medium precedence)
        foreach (var role in roleAttributes.Values)
        {
            foreach (var attr in role.Attributes)
            {
                result[attr.Key] = ConvertJsonElement(attr.Value);
            }
        }

        // 3. Override with user attributes (highest precedence)
        if (userAttributes != null)
        {
            foreach (var attr in userAttributes.Attributes)
            {
                result[attr.Key] = ConvertJsonElement(attr.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// Converts JsonElement to appropriate .NET type.
    /// </summary>
    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : (element.TryGetInt64(out var l) ? l : element.GetDecimal()),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(e => ConvertJsonElement(e)).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.ToString()
        };
    }
}
