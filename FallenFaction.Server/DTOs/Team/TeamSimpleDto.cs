// DTOs/Team/TeamSimpleDto.cs
namespace FallenFaction.Server.DTOs.Team
{
    public class TeamSimpleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? AvatarImagePath { get; set; }
        public string? BackgroundImagePath { get; set; }
    }
}