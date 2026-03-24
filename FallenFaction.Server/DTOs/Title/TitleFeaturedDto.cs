// DTOs/Title/TitleFeaturedDto.cs
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Title
{
    public class TitleFeaturedDto
    {
        public int Id { get; set; }
        public string OriginalTitle { get; set; } = string.Empty;
        public string EnglishTitle { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public MangaType Type { get; set; }
        public string? LatestChapter { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public int ChapterCount { get; set; }
        public DateTime? LastUpdated { get; set; }

        // Stats
        public double AverageRating { get; set; }
        public int ViewCount { get; set; }

        // Status & flags (needed for TitleCard display)
        public string StatusTitle { get; set; } = string.Empty;
        public string StatusTranslation { get; set; } = string.Empty;
        public int AgeRestriction { get; set; }
    }
}