using System.ComponentModel.DataAnnotations;

namespace UI.Modules.AccessControl.Models;

public class AuthorizationTestRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string WorkstreamId { get; set; } = string.Empty;

    [Required]
    public string Resource { get; set; } = string.Empty;

    [Required]
    public string Action { get; set; } = string.Empty;

    public string? MockEntityJson { get; set; }

    public string? TestDescription { get; set; }
}
