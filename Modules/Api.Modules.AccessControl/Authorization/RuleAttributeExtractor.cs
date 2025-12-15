using System.Text.Json;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Extracts attribute validation information from ABAC rule configurations.
/// Parses Configuration JSON to determine which user/resource/environment attributes each rule validates.
/// </summary>
public class RuleAttributeExtractor
{
    /// <summary>
    /// Information about an attribute validation performed by a rule.
    /// </summary>
    /// <param name="AttributeName">Name of the attribute being validated</param>
    /// <param name="Source">Where the attribute comes from: "User", "Resource", or "Environment"</param>
    /// <param name="IsConditional">Whether validation only happens under certain conditions (e.g., ValueRange threshold checks)</param>
    public record AttributeValidation(
        string AttributeName,
        string Source,
        bool IsConditional
    );

    /// <summary>
    /// Extracts all attribute validations performed by a rule based on its Configuration JSON.
    /// </summary>
    /// <param name="rule">The ABAC rule to analyze</param>
    /// <returns>List of attributes validated by this rule</returns>
    public List<AttributeValidation> ExtractValidatedAttributes(AbacRule rule)
    {
        var validations = new List<AttributeValidation>();

        if (string.IsNullOrWhiteSpace(rule.Configuration))
        {
            return validations;
        }

        try
        {
            using var config = JsonDocument.Parse(rule.Configuration);
            var root = config.RootElement;

            switch (rule.RuleType)
            {
                case "PropertyMatch":
                    // Validates: user attribute matches resource property
                    if (root.TryGetProperty("userAttribute", out var userAttr) && userAttr.GetString() is { } ua)
                    {
                        validations.Add(new AttributeValidation(ua, "User", false));
                    }
                    if (root.TryGetProperty("resourceProperty", out var resProp) && resProp.GetString() is { } rp)
                    {
                        validations.Add(new AttributeValidation(rp, "Resource", false));
                    }
                    break;

                case "AttributeComparison":
                    // Two formats: new (leftAttribute/rightProperty) and legacy (userAttribute/resourceProperty)
                    if (root.TryGetProperty("leftAttribute", out var left) && left.GetString() is { } leftAttr)
                    {
                        // New format: may have "user." or "resource." prefix
                        var attrName = leftAttr;
                        var source = "User"; // Default to user

                        if (attrName.StartsWith("user.", StringComparison.OrdinalIgnoreCase))
                        {
                            attrName = attrName.Substring(5);
                            source = "User";
                        }
                        else if (attrName.StartsWith("resource.", StringComparison.OrdinalIgnoreCase))
                        {
                            attrName = attrName.Substring(9);
                            source = "Resource";
                        }

                        validations.Add(new AttributeValidation(attrName, source, false));
                    }
                    else if (root.TryGetProperty("userAttribute", out var legacyUser) && legacyUser.GetString() is { } lu)
                    {
                        // Legacy format
                        validations.Add(new AttributeValidation(lu, "User", false));
                    }

                    if (root.TryGetProperty("rightProperty", out var right) && right.GetString() is { } rightProp)
                    {
                        validations.Add(new AttributeValidation(rightProp, "Resource", false));
                    }
                    else if (root.TryGetProperty("resourceProperty", out var legacyRes) && legacyRes.GetString() is { } lr)
                    {
                        validations.Add(new AttributeValidation(lr, "Resource", false));
                    }
                    break;

                case "ValueRange":
                    // Always validates resource property
                    if (root.TryGetProperty("resourceProperty", out var vrResProp) && vrResProp.GetString() is { } vrp)
                    {
                        validations.Add(new AttributeValidation(vrp, "Resource", false));
                    }

                    // Conditional: if threshold exceeded, validates required user attribute
                    if (root.TryGetProperty("requiredAttribute", out var reqAttr) && reqAttr.GetString() is { } ra)
                    {
                        validations.Add(new AttributeValidation(ra, "User", true)); // Conditional check
                    }
                    break;

                case "AttributeValue":
                    // Validates attribute against static value (could be user or resource, default to user)
                    if (root.TryGetProperty("attribute", out var avAttr) && avAttr.GetString() is { } ava)
                    {
                        validations.Add(new AttributeValidation(ava, "User", false));
                    }
                    break;

                case "TimeRestriction":
                    // Optional: validates resource Classification property
                    if (root.TryGetProperty("resourceClassification", out _))
                    {
                        validations.Add(new AttributeValidation("Classification", "Resource", false));
                    }
                    // Always validates time-based environment attribute
                    validations.Add(new AttributeValidation("IsBusinessHours", "Environment", false));
                    break;

                case "LocationRestriction":
                    // Validates network/IP environment attribute
                    validations.Add(new AttributeValidation("ClientIpAddress", "Environment", false));
                    break;

                default:
                    // Unknown rule type - skip
                    break;
            }
        }
        catch (JsonException)
        {
            // Invalid JSON - return empty list (logged elsewhere during evaluation)
        }

        return validations;
    }

    /// <summary>
    /// Extracts all user attributes validated by a collection of rules.
    /// </summary>
    /// <param name="rules">Rules to analyze</param>
    /// <returns>Set of unique user attribute names</returns>
    public HashSet<string> ExtractUserAttributes(IEnumerable<AbacRule> rules)
    {
        return rules
            .SelectMany(ExtractValidatedAttributes)
            .Where(v => v.Source == "User" && !v.IsConditional) // Only unconditional user attributes
            .Select(v => v.AttributeName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts all resource properties validated by a collection of rules.
    /// </summary>
    /// <param name="rules">Rules to analyze</param>
    /// <returns>Set of unique resource property names</returns>
    public HashSet<string> ExtractResourceProperties(IEnumerable<AbacRule> rules)
    {
        return rules
            .SelectMany(ExtractValidatedAttributes)
            .Where(v => v.Source == "Resource")
            .Select(v => v.AttributeName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
