using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.Graph.Models;

namespace UI.Modules.AccessControl.Models;

public class UserDetailsViewModel
{
    public Microsoft.Graph.Models.User User { get; set; } = null!;
    public List<Microsoft.Graph.Models.Group> Groups { get; set; } = [];
    public List<UserAttribute> UserAttributes { get; set; } = [];
    public List<string> RoleAssignments { get; set; } = [];
}
