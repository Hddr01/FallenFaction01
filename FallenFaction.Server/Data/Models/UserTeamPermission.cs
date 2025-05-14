namespace FallenFaction.Server.Data.Models
{
    public class UserTeamPermission
    {
        public int Id { get; set; }
        public string PermissionName { get; set; }
        public ICollection<UserTeamRolePermission> UserTeamRolePermissions { get; set; } = new List<UserTeamRolePermission>();
    }
}
