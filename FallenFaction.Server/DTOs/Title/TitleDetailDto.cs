// DTOs/Title/TitleDetailDto.cs - New detailed DTO for title pages
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Team;

namespace FallenFaction.Server.DTOs.Title
{
    public class TitleDetailDto
    {
        public int Id { get; set; }
        public string OriginalTitle { get; set; } = string.Empty;
        public string EnglishTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverImagePath { get; set; } = string.Empty;
        public string? BackgroundImagePath { get; set; }
        public MangaType Type { get; set; }
        public string StatusTitle { get; set; } = string.Empty;
        public string StatusTranslation { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public int AgeRestriction { get; set; }
        public int ChapterCount { get; set; }
        public string LatestChapter { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int BookmarkCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime? LastUpdated { get; set; }
        public List<TeamSimpleDto> Teams { get; set; } = new List<TeamSimpleDto>();
        public List<string> Authors { get; set; } = new List<string>();
        public List<string> Artists { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();

        // ── AI Translation fields ─────────────────────────────────────────────
        /// <summary>1=Translation, 2=Original, 3=Fanfic, 4=AITranslation</summary>
        public int TitleCategory { get; set; } = 1;
        /// <summary>How many AI chapters are still locked (IsAILocked=true).</summary>
        public int LockedChapterCount { get; set; } = 0;
        /// <summary>How many AI chapters are already unlocked by the community.</summary>
        public int UnlockedChapterCount { get; set; } = 0;
    }
}