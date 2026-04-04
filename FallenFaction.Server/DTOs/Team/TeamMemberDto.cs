// DTOs/Team/TeamMemberDto.cs
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Team
{
    public class TeamMemberDto
    {
        public string UserId { get; set; }
        /// <summary>Unique @handle.</summary>
        public string UserName { get; set; }
        /// <summary>Display name (ProfileName ?? UserName).</summary>
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string ProfilePicturePath { get; set; }
        public TeamRole Role { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsOnline { get; set; }
    }
}