using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Title
{
    public class TitleCatalogDto
    {
        public int Id { get; set; }
        public string OriginalTitle { get; set; } = string.Empty;
        public string EnglishTitle { get; set; } = string.Empty;
        public string? AlternativeNames { get; set; }
        public string CoverImagePath { get; set; } = string.Empty;
        public string? BackgroundImagePath { get; set; }

        // Type and Status
        public MangaType Type { get; set; }
        public string StatusTitle { get; set; } = string.Empty;
        public string StatusTranslation { get; set; } = string.Empty;
        public int AgeRestriction { get; set; }

        // Content Info
        public string Description { get; set; } = string.Empty;
        public string? LatestChapter { get; set; }
        public int ChapterCount { get; set; }

        // Dates - ReleaseDate is string in your model
        public string ReleaseDate { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; } // From latest chapter

        // Stats - Calculated from separate tables
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int BookmarkCount { get; set; }
        public int ViewCount { get; set; }

        // Related Entities (as string lists for performance)
        public List<string> Authors { get; set; } = new List<string>();
        public List<string> Artists { get; set; } = new List<string>();
        public List<string> Publishers { get; set; } = new List<string>();
        public List<string> Teams { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Formats { get; set; } = new List<string>();
    }
}
