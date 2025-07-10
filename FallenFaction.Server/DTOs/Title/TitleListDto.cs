// DTOs/Title/TitleListDto.cs
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Title
{
    public class TitleListDto
    {
        public int Id { get; set; }
        public string OriginalTitle { get; set; } = string.Empty;
        public string EnglishTitle { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public MangaType Type { get; set; }
        public string? LatestChapter { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}