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