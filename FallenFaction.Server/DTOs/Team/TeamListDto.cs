// DTOs/Team/TeamListDto.cs
namespace FallenFaction.Server.DTOs.Team
{
    public class TeamListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreatorName { get; set; }
        public int MemberCount { get; set; }
        public int TitleCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}