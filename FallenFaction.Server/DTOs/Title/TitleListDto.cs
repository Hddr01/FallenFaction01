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
        public string ReleaseDate { get; set; } = string.Empty;
        public int ChapterCount { get; set; }
        public double AverageRating { get; set; }
        public int BookmarkCount { get; set; }
        public TitleCategory TitleCategory { get; set; }
        public double LatestChapterNumber { get; set; }
    }
}