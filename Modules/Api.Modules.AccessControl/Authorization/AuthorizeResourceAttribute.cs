using Microsoft.AspNetCore.Authorization;

namespace Api.Modules.AccessControl.Authorization;

/// <summary>
/// Attribute to enforce resource-based authorization on controller actions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeResourceAttribute : AuthorizeAttribute
{
    public string Resource { get; }
    public string Action { get; }

    public AuthorizeResourceAttribute(string resource, string action)
    {
        Resource = resource;
        Action = action;
        Policy = $"Resource:{resource}:Action:{action}";
    }
}
