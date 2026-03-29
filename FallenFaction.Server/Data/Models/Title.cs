using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FallenFaction.Server.Data.Models
{
    public enum MangaType
    {
        Manga = 1,
        Manhwa = 2,
        Manhua = 3,
        Comic = 4,
        Webtoon = 5
    }

    public enum TitleCategory
    {
        Translation = 1,  // Translated from a source language
        Original = 2,     // Original work created by the group
        Fanfic = 3,       // Fan fiction based on an existing IP
        AITranslation = 4 // AI-translated novel (admin-only, ticket-gated chapters)
    }

    public class Title
    {
        public Title()
        {
            // Initialize collections
            Categories = new HashSet<Category>();
            Tags = new HashSet<Tag>();
            Formats = new HashSet<Format>();
            Authors = new HashSet<Author>();
            Artists = new HashSet<Artist>();
            Publishers = new HashSet<Publisher>();
            Teams = new HashSet<Team>();
            Chapters = new HashSet<Chapter>();
            PendingChapters = new HashSet<PendingChapter>();
            RejectedChapters = new HashSet<RejectedChapter>();
            Comments = new HashSet<Comment>();
            ChangeLogs = new HashSet<TitleChangeLog>();
            PendingTitleChanges = new HashSet<PendingTitleChange>();
            ApprovedTitleChanges = new HashSet<ApprovedTitleChange>();
            RejectedTitleChanges = new HashSet<RejectedTitleChange>();
            Ratings = new HashSet<Rating>();
            Bookmarks = new HashSet<Bookmark>();
            ExternalLinks = new List<string>();
        }

        [Key]
        public int Id { get; set; }

        [StringLength(255)]
        public string CoverImagePath { get; set; } = string.Empty;

        [StringLength(255)]
        public string BackgroundImagePath { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string OriginalTitle { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string EnglishTitle { get; set; } = string.Empty;

        public string AlternativeNames { get; set; } = string.Empty;

        public string ReleaseDate { get; set; } = string.Empty;

        [StringLength(10000)]
        public string Description { get; set; } = string.Empty;

        public string StatusTitle { get; set; } = string.Empty;

        public string StatusTranslation { get; set; } = string.Empty;

        public MangaType Type { get; set; }

        // ── Content classification ───────────────────────────────────────────────
        public TitleCategory TitleCategory { get; set; } = TitleCategory.Translation;

        // For Fanfic: reference to the original title this is based on
        public int? SourceTitleId { get; set; }          // Optional: if the source exists in-system
        [ForeignKey("SourceTitleId")]
        public Title? SourceTitle { get; set; }
        public ICollection<Title> FanficDerivatives { get; set; } = new List<Title>();

        [StringLength(500)]
        public string? SourceTitleName { get; set; }  // Free-text fallback when source isn't in-system

        public int AgeRestriction { get; set; }

        // Title availability and comment settings
        public bool IsAvailable { get; set; } = true;
        public bool AreCommentsEnabled { get; set; } = true;
        public bool AreChapterCommentsEnabled { get; set; } = true;

        public string? AdminComment { get; set; }

        // FIXED: Add missing creator and timestamp properties
        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public virtual AppUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // External links serialization
        [NotMapped]
        public ICollection<string> ExternalLinks { get; set; }

        [StringLength(1000)]
        public string ExternalLinksSerialized
        {
            get => string.Join(";", ExternalLinks);
            set => ExternalLinks = value != null ? new List<string>(value.Split(';', StringSplitOptions.RemoveEmptyEntries)) : new List<string>();
        }

        // Navigation properties
        public ICollection<Category> Categories { get; set; }
        public ICollection<Tag> Tags { get; set; }
        public ICollection<Format> Formats { get; set; }
        public ICollection<Author> Authors { get; set; }
        public ICollection<Artist> Artists { get; set; }
        public ICollection<Publisher> Publishers { get; set; }
        public ICollection<Team> Teams { get; set; }
        public ICollection<Chapter> Chapters { get; set; }
        public ICollection<PendingChapter> PendingChapters { get; set; }
        public ICollection<RejectedChapter> RejectedChapters { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public virtual ICollection<TitleChangeLog> ChangeLogs { get; set; }
        public virtual ICollection<PendingTitleChange> PendingTitleChanges { get; set; }
        public virtual ICollection<ApprovedTitleChange> ApprovedTitleChanges { get; set; }
        public virtual ICollection<RejectedTitleChange> RejectedTitleChanges { get; set; }
        public ICollection<Rating> Ratings { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; }
    }
}