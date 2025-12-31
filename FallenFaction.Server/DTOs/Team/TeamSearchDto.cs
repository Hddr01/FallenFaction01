namespace FallenFaction.Server.DTOs.Team
{
    public class TeamSearchDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MemberCount { get; set; }
        public int TitleCount { get; set; }
        public string? AvatarImagePath { get; set; }
    }
}
