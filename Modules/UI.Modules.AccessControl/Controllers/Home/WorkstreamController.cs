using Api.Modules.AccessControl.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace UI.Modules.AccessControl.Controllers.Home;

/// <summary>
/// Controller for managing workstream context selection.
/// </summary>
public class WorkstreamController(ILogger<WorkstreamController> logger) : Controller
{
    private readonly ILogger<WorkstreamController> _logger = logger;
    private const string WorkstreamSessionKey = "SelectedWorkstream";

    /// <summary>
    /// Sets the selected workstream in session and returns to the previous page.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetWorkstream(string workstreamId, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(workstreamId))
        {
            _logger.LogWarning("Attempted to set empty workstream ID");
            return BadRequest("Workstream ID cannot be empty");
        }

        HttpContext.Session.SetString(WorkstreamSessionKey, workstreamId);
        _logger.LogInformation("Workstream context set to: {WorkstreamId}", workstreamId);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Gets the currently selected workstream from session.
    /// </summary>
    public static string GetSelectedWorkstream(HttpContext httpContext)
    {
        return httpContext.Session.GetString(WorkstreamSessionKey) ?? "platform";
    }
}
