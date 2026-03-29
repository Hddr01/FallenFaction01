using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public enum TranslationRequestStatus
    {
        Pending = 1,        // User submitted, awaiting admin review
        Approved = 2,       // Admin approved, queued for processing
        PreProcessing = 3,  // Being scraped/prepared
        Released = 4,       // Live on the platform as an AI Translation title
        Rejected = 5        // Admin rejected with reason
    }

    /// <summary>
    /// A user's request to have a novel added to the platform for AI translation.
    /// Mirrors WTR-Lab's Series Request system.
    /// Flow: Pending → Approved → PreProcessing → Released (or Rejected at any stage).
    /// </summary>
    public class TranslationRequest
    {
        [Key]
        public int Id { get; set; }

        // ── Who requested it ─────────────────────────────────────────────────
        [Required]
        public string RequestedByUserId { get; set; } = string.Empty;

        [ForeignKey("RequestedByUserId")]
        public AppUser RequestedByUser { get; set; } = null!;

        // ── What they're requesting ──────────────────────────────────────────
        /// <summary>URL to the raw source novel (e.g. a Chinese novel site).</summary>
        [Required]
        [StringLength(1000)]
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>User-provided English title for the novel.</summary>
        [Required]
        [StringLength(255)]
        public string ProposedTitle { get; set; } = string.Empty;

        /// <summary>Original language title, if known.</summary>
        [StringLength(255)]
        public string? OriginalLanguageTitle { get; set; }

        /// <summary>Brief description or synopsis provided by the requester.</summary>
        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>Comma-separated genre names (at least 1 required).</summary>
        [Required]
        [StringLength(500)]
        public string Genres { get; set; } = string.Empty;

        /// <summary>Comma-separated tag names (at least 2 required).</summary>
        [Required]
        [StringLength(1000)]
        public string Tags { get; set; } = string.Empty;

        /// <summary>Optional cover image URL from the source.</summary>
        [StringLength(1000)]
        public string? CoverImageUrl { get; set; }

        /// <summary>Approximate total chapter count from the source, if known.</summary>
        public int? EstimatedChapterCount { get; set; }

        // ── Status & workflow ────────────────────────────────────────────────
        public TranslationRequestStatus Status { get; set; } = TranslationRequestStatus.Pending;

        /// <summary>Admin who reviewed this request.</summary>
        public string? ReviewedByUserId { get; set; }

        [ForeignKey("ReviewedByUserId")]
        public AppUser? ReviewedByUser { get; set; }

        /// <summary>Reason for rejection, if rejected.</summary>
        [StringLength(1000)]
        public string? RejectionReason { get; set; }

        /// <summary>Admin notes (internal, not shown to requester).</summary>
        [StringLength(1000)]
        public string? AdminNotes { get; set; }

        /// <summary>If released, the Title that was created from this request.</summary>
        public int? ReleasedTitleId { get; set; }

        [ForeignKey("ReleasedTitleId")]
        public Title? ReleasedTitle { get; set; }

        /// <summary>How many tickets were spent to release this (if fast-released by user).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ReleaseTicketCost { get; set; }

        // ── Timestamps ───────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
