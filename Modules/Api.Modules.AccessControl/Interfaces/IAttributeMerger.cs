using Api.Modules.AccessControl.Models;

namespace Api.Modules.AccessControl.Interfaces;

/// <summary>
/// Service responsible for merging attributes from multiple sources.
/// Implements precedence rule: User > Role > Group.
/// </summary>
public interface IAttributeMerger
{
    /// <summary>
    /// Merges attributes from group, role, and user sources with proper precedence.
    /// User attributes override role attributes, which override group attributes.
    /// </summary>
    /// <param name="groupAttributes">Attributes from user's groups</param>
    /// <param name="roleAttributes">Attributes from user's roles</param>
    /// <param name="userAttributes">Attributes specific to the user</param>
    /// <returns>Merged attribute dictionary with User > Role > Group precedence</returns>
    Dictionary<string, object> MergeAttributes(
        IDictionary<string, GroupAttributes> groupAttributes,
        IDictionary<string, RoleAttributes> roleAttributes,
        UserAttributes? userAttributes);
}
