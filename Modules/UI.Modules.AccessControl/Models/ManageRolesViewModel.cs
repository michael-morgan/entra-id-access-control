using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Models;

public class ManageRolesViewModel
{
    public User User { get; set; } = null!;
    public List<Group> Groups { get; set; } = new();
    public List<string> Workstreams { get; set; } = new();
    public Dictionary<string, List<string>> AvailableRoles { get; set; } = new();
    public Dictionary<string, List<string>> CurrentRoleAssignments { get; set; } = new();
}
