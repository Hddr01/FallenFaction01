// DTOs/Team/TeamTopDto.cs
namespace FallenFaction.Server.DTOs.Team
{
    public class TeamTopDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Progress { get; set; }
        public string Score { get; set; } = string.Empty;
    }
}