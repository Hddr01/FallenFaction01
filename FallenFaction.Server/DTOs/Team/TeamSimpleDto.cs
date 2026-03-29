// DTOs/Team/TeamSimpleDto.cs
namespace FallenFaction.Server.DTOs.Team
{
    public class TeamSimpleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarImagePath { get; set; }
        public string? BackgroundImagePath { get; set; }
        /// <summary>True for the AI/TL system team — used on frontend to hide it from join-request eligibility.</summary>
        public bool IsSystemTeam { get; set; }
    }
}