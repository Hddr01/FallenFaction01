// DTOs/Team/TeamMemberDto.cs
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Team
{
    public class TeamMemberDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string ProfilePicturePath { get; set; }
        public TeamRole Role { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsOnline { get; set; }
    }
}