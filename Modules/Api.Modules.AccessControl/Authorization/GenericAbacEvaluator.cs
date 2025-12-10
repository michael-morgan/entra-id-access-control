using System.Text.Json;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Models;
using Api.Modules.AccessControl.Persistence;
using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Generic ABAC evaluator that processes declarative rules from the database.
/// Supports rule groups with AND/OR logic and multiple rule types.
/// </summary>
public class GenericAbacEvaluator(
    string workstreamId,
    AccessControlDbContext context,
    ILogger<GenericAbacEvaluator> logger) : IWorkstreamAbacEvaluator
{
    private readonly AccessControlDbContext _context = context;
    private readonly ILogger<GenericAbacEvaluator> _logger = logger;

    public string WorkstreamId { get; } = workstreamId;

    public async Task<AbacEvaluationResult?> EvaluateAsync(
        AbacContext context,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[GENERIC ABAC] Evaluating {Workstream} - {Resource}:{Action}",
            WorkstreamId, resource, action);

        // Load all rule groups for this workstream, then filter by resource/action pattern matching
        // We need to do pattern matching in memory because SQL doesn't support wildcard matching in WHERE clause
        var allRuleGroups = await _context.AbacRuleGroups
            .Where(g => g.WorkstreamId == WorkstreamId && g.IsActive)
            .Include(g => g.Rules.Where(r => r.IsActive))
            .Include(g => g.ChildGroups.Where(c => c.IsActive))
            .OrderBy(g => g.Priority)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        // Filter by resource and action patterns (supports wildcards like "Loan/*")
        var ruleGroups = allRuleGroups
            .Where(g => (g.Resource == null || ResourceMatches(resource, g.Resource)) &&
                       (g.Action == null || g.Action == action))
            .ToList();

        _logger.LogInformation("[GENERIC ABAC] Found {Count} rule groups for {Workstream} - {Resource}:{Action}",
            ruleGroups.Count, WorkstreamId, resource, action);

        if (ruleGroups.Count == 0)
        {
            _logger.LogInformation("[GENERIC ABAC] No rule groups found for {Workstream} - {Resource}:{Action}",
                WorkstreamId, resource, action);
            return null; // No declarative rules configured
        }

        // Evaluate top-level groups (those without parent)
        var topLevelGroups = ruleGroups.Where(g => g.ParentGroupId == null).ToList();

        foreach (var group in topLevelGroups)
        {
            var result = await EvaluateRuleGroupAsync(group, context, resource, action, ruleGroups, cancellationToken);

            if (result != null && !result.Allowed)
            {
                // First denial wins
                return result;
            }
        }

        // All groups passed or returned null
        return AbacEvaluationResult.Allow("All declarative rule groups passed");
    }

    private async Task<AbacEvaluationResult?> EvaluateRuleGroupAsync(
        AbacRuleGroup group,
        AbacContext context,
        string resource,
        string action,
        List<AbacRuleGroup> allGroups,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Evaluating rule group: {GroupName} ({Operator})", group.GroupName, group.LogicalOperator);

        var results = new List<AbacEvaluationResult>();

        // Evaluate direct rules in this group
        foreach (var rule in group.Rules.Where(r => r.IsActive).OrderBy(r => r.Priority))
        {
            var ruleResult = await EvaluateRuleAsync(rule, context, resource, action, cancellationToken);
            if (ruleResult != null)
            {
                results.Add(ruleResult);
            }
        }

        // Evaluate child groups
        var childGroups = allGroups.Where(g => g.ParentGroupId == group.Id).ToList();
        foreach (var childGroup in childGroups)
        {
            var childResult = await EvaluateRuleGroupAsync(childGroup, context, resource, action, allGroups, cancellationToken);
            if (childResult != null)
            {
                results.Add(childResult);
            }
        }

        if (results.Count == 0)
        {
            return null; // No applicable rules
        }

        // Apply logical operator
        if (group.LogicalOperator.Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            // ALL must pass
            var firstFailure = results.FirstOrDefault(r => !r.Allowed);
            if (firstFailure != null)
            {
                return AbacEvaluationResult.Deny(
                    $"Rule group '{group.GroupName}' failed: {firstFailure.Reason}",
                    firstFailure.Message);
            }

            return AbacEvaluationResult.Allow($"Rule group '{group.GroupName}' passed (AND)");
        }
        else if (group.LogicalOperator.Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            // ANY must pass
            var firstSuccess = results.FirstOrDefault(r => r.Allowed);
            if (firstSuccess != null)
            {
                return AbacEvaluationResult.Allow($"Rule group '{group.GroupName}' passed (OR): {firstSuccess.Reason}");
            }

            var reasons = string.Join("; ", results.Select(r => r.Reason));
            return AbacEvaluationResult.Deny(
                $"Rule group '{group.GroupName}' failed - no rules passed: {reasons}",
                group.Description ?? "Access denied by business rules");
        }

        return null;
    }

    private Task<AbacEvaluationResult?> EvaluateRuleAsync(
        AbacRule rule,
        AbacContext context,
        string resource,
        string action,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Evaluating rule: {RuleName} ({RuleType})", rule.RuleName, rule.RuleType);

        try
        {
            var config = JsonDocument.Parse(rule.Configuration);

            return rule.RuleType switch
            {
                "AttributeComparison" => Task.FromResult<AbacEvaluationResult?>(EvaluateAttributeComparison(rule, config, context)),
                "PropertyMatch" => Task.FromResult<AbacEvaluationResult?>(EvaluatePropertyMatch(rule, config, context)),
                "ValueRange" => Task.FromResult<AbacEvaluationResult?>(EvaluateValueRange(rule, config, context)),
                "TimeRestriction" => Task.FromResult<AbacEvaluationResult?>(EvaluateTimeRestriction(rule, config, context)),
                "LocationRestriction" => Task.FromResult<AbacEvaluationResult?>(EvaluateLocationRestriction(rule, config, context)),
                "AttributeValue" => Task.FromResult<AbacEvaluationResult?>(EvaluateAttributeValue(rule, config, context)),
                _ => Task.FromResult<AbacEvaluationResult?>(null)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rule {RuleName}: {Message}", rule.RuleName, ex.Message);
            return Task.FromResult<AbacEvaluationResult?>(null);
        }
    }

    private AbacEvaluationResult? EvaluateAttributeComparison(AbacRule rule, JsonDocument config, AbacContext context)
    {
        // Supports two formats:
        // New format: { "leftAttribute": "user.ApprovalLimit", "operator": "greaterThanOrEqual", "rightProperty": "RequestedAmount" }
        // Legacy format: { "userAttribute": "ApprovalLimit", "operator": ">=", "resourceProperty": "Amount" }

        string? leftAttr, op, rightProp;

        // Try new format first
        if (config.RootElement.TryGetProperty("leftAttribute", out var leftAttrElement))
        {
            leftAttr = leftAttrElement.GetString();
            op = config.RootElement.GetProperty("operator").GetString();
            rightProp = config.RootElement.GetProperty("rightProperty").GetString();
        }
        // Fall back to legacy format
        else
        {
            leftAttr = "user." + config.RootElement.GetProperty("userAttribute").GetString();
            op = config.RootElement.GetProperty("operator").GetString();
            rightProp = config.RootElement.GetProperty("resourceProperty").GetString();
        }

        var leftValue = GetContextValue(context, leftAttr);
        var rightValue = GetContextValue(context, rightProp);

        if (leftValue == null || rightValue == null)
        {
            return null; // Cannot evaluate without values
        }

        var result = CompareValues(leftValue, op!, rightValue);

        if (!result)
        {
            return AbacEvaluationResult.Deny(
                $"Rule '{rule.RuleName}' failed: {leftAttr} ({leftValue}) {op} {rightProp} ({rightValue})",
                rule.FailureMessage ?? $"Attribute comparison failed: {leftAttr} {op} {rightProp}");
        }

        return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed");
    }

    private AbacEvaluationResult? EvaluatePropertyMatch(AbacRule rule, JsonDocument config, AbacContext context)
    {
        // Example: { "userAttribute": "Region", "operator": "==", "resourceProperty": "Region", "allowWildcard": "ALL" }
        var userAttr = config.RootElement.GetProperty("userAttribute").GetString();
        var op = config.RootElement.GetProperty("operator").GetString();
        var resourceProp = config.RootElement.GetProperty("resourceProperty").GetString();

        _logger.LogInformation("[PROPERTY MATCH] Rule '{RuleName}': Checking {UserAttr} {Op} {ResourceProp}",
            rule.RuleName, userAttr, op, resourceProp);

        // Get user value from user attributes
        object? userValue = null;
        if (!string.IsNullOrEmpty(userAttr))
        {
            context.UserAttributes.TryGetValue(userAttr, out userValue);
        }

        // Get resource value from resource attributes
        object? resourceValue = null;
        if (!string.IsNullOrEmpty(resourceProp))
        {
            context.ResourceAttributes.TryGetValue(resourceProp, out resourceValue);
        }

        _logger.LogInformation("[PROPERTY MATCH] Values: userValue={UserValue}, resourceValue={ResourceValue}",
            userValue, resourceValue);

        if (userValue == null || resourceValue == null)
        {
            _logger.LogInformation("[PROPERTY MATCH] Skipping rule - missing values");
            return null; // Cannot evaluate
        }

        // Check for wildcard bypass
        if (config.RootElement.TryGetProperty("allowWildcard", out var wildcardElement))
        {
            var wildcardValue = wildcardElement.GetString();
            _logger.LogInformation("[PROPERTY MATCH] Checking wildcard: allowWildcard={WildcardValue}, userValue={UserValue}",
                wildcardValue, userValue);
            if (!string.IsNullOrEmpty(wildcardValue) &&
                userValue.ToString()!.Equals(wildcardValue, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[PROPERTY MATCH] Rule '{RuleName}' passed via wildcard: {UserAttr}={WildcardValue}",
                    rule.RuleName, userAttr, wildcardValue);
                return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed (wildcard bypass: {userAttr}={wildcardValue})");
            }
        }

        var result = CompareValues(userValue, op!, resourceValue);

        _logger.LogInformation("[PROPERTY MATCH] Comparison result: {Result} ({UserValue} {Op} {ResourceValue})",
            result, userValue, op, resourceValue);

        if (!result)
        {
            return AbacEvaluationResult.Deny(
                $"Rule '{rule.RuleName}' failed: {userAttr} ({userValue}) {op} {resourceProp} ({resourceValue})",
                rule.FailureMessage ?? "Property match failed");
        }

        return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed");
    }

    private AbacEvaluationResult? EvaluateValueRange(AbacRule rule, JsonDocument config, AbacContext context)
    {
        // Support two schemas:
        // 1. Simple range: { "resourceProperty": "Amount", "min": 0, "max": 10000 }
        // 2. Threshold-based: { "resourceProperty": "Amount", "threshold": 50000, "requiredAttribute": "ManagementLevel", "minValue": 2 }

        var resourceProp = config.RootElement.GetProperty("resourceProperty").GetString();
        var value = GetContextValue(context, resourceProp);

        if (value == null)
        {
            return null;
        }

        var numValue = Convert.ToDecimal(value);

        // Check if this is threshold-based conditional logic
        if (config.RootElement.TryGetProperty("threshold", out var thresholdElement))
        {
            var threshold = thresholdElement.GetDecimal();
            var requiredAttr = config.RootElement.GetProperty("requiredAttribute").GetString();
            var minValue = config.RootElement.GetProperty("minValue").GetDecimal();

            // If resource value exceeds threshold, check that required attribute meets minimum
            if (numValue > threshold)
            {
                var attrValue = GetContextValue(context, requiredAttr);

                if (attrValue == null)
                {
                    return AbacEvaluationResult.Deny(
                        $"Rule '{rule.RuleName}' failed: {resourceProp} ({numValue}) exceeds threshold ({threshold}), but {requiredAttr} is not available",
                        rule.FailureMessage ?? $"{resourceProp} over {threshold} requires {requiredAttr}");
                }

                var attrNumValue = Convert.ToDecimal(attrValue);

                if (attrNumValue < minValue)
                {
                    return AbacEvaluationResult.Deny(
                        $"Rule '{rule.RuleName}' failed: {resourceProp} ({numValue}) exceeds threshold ({threshold}), but {requiredAttr} ({attrNumValue}) < required minimum ({minValue})",
                        rule.FailureMessage ?? $"{resourceProp} over {threshold} requires {requiredAttr} of at least {minValue}");
                }

                _logger.LogDebug("Rule '{RuleName}' passed: {ResourceProp} ({NumValue}) > threshold ({Threshold}) and {RequiredAttr} ({AttrValue}) >= {MinValue}",
                    rule.RuleName, resourceProp, numValue, threshold, requiredAttr, attrNumValue, minValue);
            }

            return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed");
        }

        // Legacy min/max range checking
        if (config.RootElement.TryGetProperty("min", out var minElement) &&
            config.RootElement.TryGetProperty("max", out var maxElement))
        {
            var min = minElement.GetDecimal();
            var max = maxElement.GetDecimal();

            if (numValue < min || numValue > max)
            {
                return AbacEvaluationResult.Deny(
                    $"Rule '{rule.RuleName}' failed: {resourceProp} ({numValue}) not in range [{min}, {max}]",
                    rule.FailureMessage ?? $"Value must be between {min} and {max}");
            }

            return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed");
        }

        // No valid configuration found
        _logger.LogWarning("Rule '{RuleName}' has invalid ValueRange configuration", rule.RuleName);
        return null;
    }

    private AbacEvaluationResult? EvaluateTimeRestriction(AbacRule rule, JsonDocument config, AbacContext context)
    {
        // Support two schemas:
        // 1. Simple business hours: Uses context.IsBusinessHours
        // 2. Advanced: { "resourceClassification": "Confidential", "allowedHours": { "start": 8, "end": 18 }, "timezone": "UTC" }

        // Check for resource classification restriction
        if (config.RootElement.TryGetProperty("resourceClassification", out var classificationElement))
        {
            var requiredClassification = classificationElement.GetString();
            var resourceClassification = context.GetResourceAttribute<string>("Classification");

            // Only apply time restriction if resource matches the classification
            if (!string.IsNullOrEmpty(requiredClassification) &&
                !requiredClassification.Equals(resourceClassification, StringComparison.OrdinalIgnoreCase))
            {
                // Resource doesn't match classification, rule doesn't apply
                _logger.LogDebug("Rule '{RuleName}' skipped: resource classification '{ResourceClassification}' doesn't match required classification '{RequiredClassification}'",
                    rule.RuleName, resourceClassification, requiredClassification);
                return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed (classification mismatch)");
            }

            // Resource matches classification, check time restrictions
            if (config.RootElement.TryGetProperty("allowedHours", out var allowedHoursElement))
            {
                var startHour = allowedHoursElement.GetProperty("start").GetInt32();
                var endHour = allowedHoursElement.GetProperty("end").GetInt32();

                // Get timezone (default to UTC if not specified)
                var timezone = "UTC";
                if (config.RootElement.TryGetProperty("timezone", out var timezoneElement))
                {
                    timezone = timezoneElement.GetString() ?? "UTC";
                }

                // Get current time in the specified timezone
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
                var currentHour = currentTime.Hour;

                _logger.LogDebug("TimeRestriction check: current hour={CurrentHour} (timezone={Timezone}), allowed={Start}-{End}",
                    currentHour, timezone, startHour, endHour);

                if (currentHour < startHour || currentHour >= endHour)
                {
                    return AbacEvaluationResult.Deny(
                        $"Rule '{rule.RuleName}' failed: current time ({currentHour}:00 {timezone}) is outside allowed hours ({startHour}:00-{endHour}:00)",
                        rule.FailureMessage ?? $"Access to {requiredClassification} resources is only allowed between {startHour}:00 and {endHour}:00 {timezone}");
                }

                return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed");
            }
        }

        // Fallback to simple business hours check
        if (!context.IsBusinessHours)
        {
            return AbacEvaluationResult.Deny(
                $"Rule '{rule.RuleName}' failed: not during allowed hours",
                rule.FailureMessage ?? "This action is only allowed during business hours");
        }

        return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed");
    }

    private static AbacEvaluationResult? EvaluateLocationRestriction(AbacRule rule, JsonDocument config, AbacContext context)
    {
        // Example: { "allowedNetworks": ["10.0.0.0/8", "192.168.1.0/24"] }
        // Simplified - would need IP address checking logic
        return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed (location check not fully implemented)");
    }

    private AbacEvaluationResult? EvaluateAttributeValue(AbacRule rule, JsonDocument config, AbacContext context)
    {
        // Example: { "attribute": "Region", "operator": "==", "value": "US-EAST" }
        var attribute = config.RootElement.GetProperty("attribute").GetString();
        var op = config.RootElement.GetProperty("operator").GetString();
        var expectedValue = config.RootElement.GetProperty("value");

        var actualValue = GetContextValue(context, attribute);

        if (actualValue == null)
        {
            return null;
        }

        var expected = expectedValue.ValueKind switch
        {
            JsonValueKind.String => (object?)expectedValue.GetString(),
            JsonValueKind.Number => (object?)expectedValue.GetDecimal(),
            JsonValueKind.True => (object?)true,
            JsonValueKind.False => (object?)false,
            _ => (object?)expectedValue.ToString()
        };

        var result = CompareValues(actualValue, op!, expected!);

        if (!result)
        {
            return AbacEvaluationResult.Deny(
                $"Rule '{rule.RuleName}' failed: {attribute} ({actualValue}) {op} expected ({expected})",
                rule.FailureMessage ?? $"Attribute check failed: {attribute}");
        }

        return AbacEvaluationResult.Allow($"Rule '{rule.RuleName}' passed");
    }

    /// <summary>
    /// Gets a value from the ABAC context by property name.
    /// Supports dynamic attribute lookup from UserAttributes and ResourceAttributes dictionaries.
    /// Supports prefixes: "user.PropertyName", "resource.PropertyName", or plain "PropertyName" (checks user first, then resource).
    /// </summary>
    private static object? GetContextValue(AbacContext context, string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return null;

        // Check for explicit prefix to avoid ambiguity
        if (propertyName.StartsWith("user.", StringComparison.OrdinalIgnoreCase))
        {
            var attrName = propertyName.Substring(5); // Remove "user." prefix
            context.UserAttributes.TryGetValue(attrName, out var userValue);
            return userValue;
        }

        if (propertyName.StartsWith("resource.", StringComparison.OrdinalIgnoreCase))
        {
            var attrName = propertyName.Substring(9); // Remove "resource." prefix
            context.ResourceAttributes.TryGetValue(attrName, out var resourceValue);
            return resourceValue;
        }

        // No prefix - try resource attributes first (safer default for most rules)
        // Resource-specific rules should take precedence to avoid user attribute shadowing
        if (context.ResourceAttributes.TryGetValue(propertyName, out var resValue))
            return resValue;

        // Then try user attributes
        if (context.UserAttributes.TryGetValue(propertyName, out var usrValue))
            return usrValue;

        // Try environment attributes (hardcoded - computed at runtime, not from database)
        return propertyName switch
        {
            "IsBusinessHours" => context.IsBusinessHours,
            "IsInternalNetwork" => context.IsInternalNetwork,
            "RequestTime" => context.RequestTime,
            "ClientIpAddress" => context.ClientIpAddress,
            _ => null
        };
    }

    private bool CompareValues(object left, string op, object right)
    {
        // Unwrap JsonElement values if present
        left = UnwrapJsonElement(left) ?? left;
        right = UnwrapJsonElement(right) ?? right;

        // Handle null comparisons
        if (left == null && right == null)
            return op == "==" || op == "equals";
        if (left == null || right == null)
            return op == "!=" || op == "notEquals";

        // Try numeric comparison first
        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDecimal(left);
            var rightNum = Convert.ToDecimal(right);

            return op switch
            {
                "==" or "equals" => leftNum == rightNum,
                "!=" or "notEquals" => leftNum != rightNum,
                ">" or "greaterThan" => leftNum > rightNum,
                ">=" or "greaterThanOrEqual" => leftNum >= rightNum,
                "<" or "lessThan" => leftNum < rightNum,
                "<=" or "lessThanOrEqual" => leftNum <= rightNum,
                _ => false
            };
        }

        // String comparison
        var leftStr = left.ToString() ?? "";
        var rightStr = right.ToString() ?? "";

        return op switch
        {
            "==" or "equals" => leftStr.Equals(rightStr, StringComparison.OrdinalIgnoreCase),
            "!=" or "notEquals" => !leftStr.Equals(rightStr, StringComparison.OrdinalIgnoreCase),
            "contains" => leftStr.Contains(rightStr, StringComparison.OrdinalIgnoreCase),
            "startsWith" => leftStr.StartsWith(rightStr, StringComparison.OrdinalIgnoreCase),
            "endsWith" => leftStr.EndsWith(rightStr, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool IsNumeric(object value)
    {
        return value is int or long or decimal or double or float
               || decimal.TryParse(value?.ToString(), out _);
    }

    /// <summary>
    /// Unwraps a JsonElement to its underlying value.
    /// Used to handle values from JSON deserialization that are still wrapped as JsonElement.
    /// </summary>
    private static object? UnwrapJsonElement(object? value)
    {
        if (value is not JsonElement jsonElement)
            return value;

        return jsonElement.ValueKind switch
        {
            JsonValueKind.Number => jsonElement.TryGetDecimal(out var d) ? d : jsonElement.GetDouble(),
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value
        };
    }

    /// <summary>
    /// Checks if a resource matches a resource pattern.
    /// Supports wildcard patterns like "Loan/*" matching "Loan/123-456-789".
    /// </summary>
    /// <param name="resource">The actual resource being accessed (e.g., "Loan/67d4de7c-...")</param>
    /// <param name="pattern">The pattern to match against (e.g., "Loan/*" or "Loan")</param>
    /// <returns>True if the resource matches the pattern</returns>
    private static bool ResourceMatches(string resource, string pattern)
    {
        // Exact match
        if (resource == pattern)
            return true;

        // Wildcard pattern matching
        // "Loan/*" matches "Loan/123", "Loan/abc", etc.
        // "Loan" matches "Loan" exactly
        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern[..^2]; // Remove "/*" suffix
            return resource.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase) ||
                   resource.Equals(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // "*" matches everything
        if (pattern == "*")
            return true;

        return false;
    }
}
