namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Service responsible for providing environment-related context information.
/// Includes business hours, network location, and timing information.
/// </summary>
public interface IEnvironmentContextProvider
{
    /// <summary>
    /// Determines if the given time falls within configured business hours.
    /// </summary>
    /// <param name="time">The time to check</param>
    /// <returns>True if within business hours, false otherwise</returns>
    bool IsWithinBusinessHours(DateTimeOffset time);

    /// <summary>
    /// Determines if the given IP address is within configured internal network ranges.
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <returns>True if internal network, false otherwise</returns>
    bool IsInternalNetwork(string? ipAddress);
}
