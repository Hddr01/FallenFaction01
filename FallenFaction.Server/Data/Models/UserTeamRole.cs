namespace FallenFaction.Server.Data.Models
{
    public class UserTeamRole
    {
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public int TeamId { get; set; }
        public Team Team { get; set; }

        public TeamRole Role { get; set; }

        // Added: Track when user joined the team
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

        public ICollection<UserTeamRolePermission> UserTeamRolePermissions { get; set; } = new List<UserTeamRolePermission>();
    }
}