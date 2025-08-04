// DTOs/Title/TitleUpdateDto.cs
namespace FallenFaction.Server.DTOs.Title
{
    public class TitleUpdateDto
    {
        public int Id { get; set; }
        public string OriginalTitle { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string LatestChapter { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}