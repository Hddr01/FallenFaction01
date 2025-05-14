namespace FallenFaction.Server.Data.Models
{
    public class UserTeamRolePermission
    {
        public string AppUserId { get; set; }
        public int TeamId { get; set; }
        public UserTeamRole UserTeamRole { get; set; }

        public int PermissionId { get; set; }
        public UserTeamPermission UserTeamPermission { get; set; }
    }
}