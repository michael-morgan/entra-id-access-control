using Api.Modules.AccessControl.Interfaces;
using Microsoft.Extensions.Options;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Service responsible for providing environment-related context information.
/// Includes business hours, network location, and timing information.
/// </summary>
public class EnvironmentContextProvider(IOptions<AuthorizationOptions> options) : IEnvironmentContextProvider
{
    private readonly IOptions<AuthorizationOptions> _options = options;

    /// <inheritdoc />
    public bool IsWithinBusinessHours(DateTimeOffset time)
    {
        var opts = _options.Value;
        var localTime = time.ToLocalTime();
        var hour = localTime.Hour;

        return hour >= opts.BusinessHoursStart && hour < opts.BusinessHoursEnd;
    }

    /// <inheritdoc />
    public bool IsInternalNetwork(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        var opts = _options.Value;

        // Check against configured internal network ranges
        foreach (var range in opts.InternalNetworkRanges)
        {
            if (ipAddress.StartsWith(range))
                return true;
        }

        return false;
    }
}
