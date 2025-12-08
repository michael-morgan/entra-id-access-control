using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Models;

public class UserDetailsViewModel
{
    public User User { get; set; } = null!;
    public List<Group> Groups { get; set; } = new();
    public List<UserAttribute> UserAttributes { get; set; } = new();
    public List<string> RoleAssignments { get; set; } = new();
}
