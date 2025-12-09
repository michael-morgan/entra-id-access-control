using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Models;

public class ManageRolesViewModel
{
    public User User { get; set; } = null!;
    public List<Group> Groups { get; set; } = [];
    public List<string> Workstreams { get; set; } = [];
    public Dictionary<string, List<string>> AvailableRoles { get; set; } = [];
    public Dictionary<string, List<string>> CurrentRoleAssignments { get; set; } = [];
}
