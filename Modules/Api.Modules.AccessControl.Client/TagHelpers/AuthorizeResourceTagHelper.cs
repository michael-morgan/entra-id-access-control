using Api.Modules.AccessControl.Client.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Api.Modules.AccessControl.Client.TagHelpers;

/// <summary>
/// Tag helper for conditionally rendering content based on authorization.
/// Usage: <authorize-resource resource="Loan/123" action="approve">...</authorize-resource>
/// </summary>
[HtmlTargetElement("authorize-resource")]
public class AuthorizeResourceTagHelper : TagHelper
{
    private readonly IAccessControlClient _client;
    private readonly ILogger<AuthorizeResourceTagHelper> _logger;

    public AuthorizeResourceTagHelper(
        IAccessControlClient client,
        ILogger<AuthorizeResourceTagHelper> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// The resource being accessed (required).
    /// Example: "Loan/123", "Document/*"
    /// </summary>
    [HtmlAttributeName("resource")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// The action being performed (required).
    /// Example: "read", "approve", "delete"
    /// </summary>
    [HtmlAttributeName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Optional workstream ID. If not provided, uses default from configuration.
    /// </summary>
    [HtmlAttributeName("workstream")]
    public string? WorkstreamId { get; set; }

    /// <summary>
    /// Optional entity data for ABAC evaluation (JSON object).
    /// </summary>
    [HtmlAttributeName("entity-data")]
    public object? EntityData { get; set; }

    /// <summary>
    /// Whether to hide the element (display: none) or remove it from DOM.
    /// Default: false (removes from DOM)
    /// </summary>
    [HtmlAttributeName("hide-only")]
    public bool HideOnly { get; set; } = false;

    /// <summary>
    /// Inverted logic: show content if NOT authorized.
    /// Useful for showing "Request Access" links.
    /// Default: false
    /// </summary>
    [HtmlAttributeName("invert")]
    public bool Invert { get; set; } = false;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Resource) || string.IsNullOrWhiteSpace(Action))
        {
            _logger.LogWarning("AuthorizeResourceTagHelper: Resource and Action are required");
            output.SuppressOutput();
            return;
        }

        try
        {
            var isAuthorized = await _client.IsAuthorizedAsync(
                Resource,
                Action,
                WorkstreamId,
                EntityData
            );

            // Apply invert logic
            var shouldShow = Invert ? !isAuthorized : isAuthorized;

            if (!shouldShow)
            {
                if (HideOnly)
                {
                    // Hide with CSS (element remains in DOM)
                    output.TagName = "div";
                    output.Attributes.Add("style", "display: none;");
                    _logger.LogDebug(
                        "Authorization denied, hiding content: Resource={Resource}, Action={Action}",
                        Resource,
                        Action
                    );
                }
                else
                {
                    // Remove from DOM
                    output.SuppressOutput();
                    _logger.LogDebug(
                        "Authorization denied, suppressing content: Resource={Resource}, Action={Action}",
                        Resource,
                        Action
                    );
                }
            }
            else
            {
                // Authorized: remove tag helper element, render content only
                output.TagName = null;
                _logger.LogDebug(
                    "Authorization allowed, rendering content: Resource={Resource}, Action={Action}",
                    Resource,
                    Action
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking authorization in tag helper: Resource={Resource}, Action={Action}",
                Resource,
                Action
            );

            // Fail-secure: deny on error
            output.SuppressOutput();
        }
    }
}
