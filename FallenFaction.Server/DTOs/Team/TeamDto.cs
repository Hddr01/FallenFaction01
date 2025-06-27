// DTOs/Team/TeamDto.cs
namespace FallenFaction.Server.DTOs.Team
{
    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreatorId { get; set; }
        public string CreatorName { get; set; }
        public DateTime CreatedDate { get; set; }
        public int MemberCount { get; set; }
        public int TitleCount { get; set; }
        public List<TeamMemberDto> Members { get; set; } = new List<TeamMemberDto>();
    }
}