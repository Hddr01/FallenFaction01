namespace FallenFaction.Server.Data.Models
{
public class UserTeamRole
{
    public string AppUserId { get; set; }
    public AppUser AppUser { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; }
    public TeamRole Role { get; set; }
    public ICollection<UserTeamRolePermission> UserTeamRolePermissions { get; set; } = new List<UserTeamRolePermission>();
}
}
