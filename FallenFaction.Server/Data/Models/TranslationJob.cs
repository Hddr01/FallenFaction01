using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public enum TranslationJobStatus
    {
        Queued = 1,       // Waiting for a worker
        Claimed = 2,      // A worker holds the lease and is translating
        NeedsReview = 3,  // Translated, but QA flagged it — admin must publish or retry
        Published = 4,    // Live as a Chapter
        Failed = 5        // Out of attempts — blocks later chapters of the same title
    }

    /// <summary>
    /// One raw chapter waiting to be (or already) machine-translated by an external worker.
    /// Jobs of the same title are processed strictly in (Volume, Chapter) order so each
    /// translation can see the summaries and glossary produced by the chapters before it.
    /// </summary>
    public class TranslationJob
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TitleId { get; set; }

        [ForeignKey("TitleId")]
        public Title Title { get; set; } = null!;

        public int VolumeNumber { get; set; } = 1;

        public int ChapterNumber { get; set; }

        // ── Source ───────────────────────────────────────────────────────────
        [StringLength(500)]
        public string? SourceChapterTitle { get; set; }

        [Required]
        public string SourceText { get; set; } = string.Empty;

        // ── Lease / workflow ─────────────────────────────────────────────────
        public TranslationJobStatus Status { get; set; } = TranslationJobStatus.Queued;

        public int? ClaimedByWorkerId { get; set; }

        [ForeignKey("ClaimedByWorkerId")]
        public TranslationWorker? ClaimedByWorker { get; set; }

        public DateTime? LeaseExpiresAt { get; set; }

        public int Attempts { get; set; } = 0;

        [StringLength(2000)]
        public string? LastError { get; set; }

        // ── Result ───────────────────────────────────────────────────────────
        [StringLength(255)]
        public string? TranslatedTitle { get; set; }

        public string? TranslatedContent { get; set; }

        /// <summary>Short plot summary of this chapter — fed to later chapters as context.</summary>
        [StringLength(4000)]
        public string? Summary { get; set; }

        /// <summary>Raw JSON QA report from the worker (review score, issues, validator warnings).</summary>
        public string? QaReport { get; set; }

        [Column(TypeName = "decimal(4,2)")]
        public decimal? QaScore { get; set; }

        [StringLength(200)]
        public string? ModelInfo { get; set; }

        public int? ResultChapterId { get; set; }

        [ForeignKey("ResultChapterId")]
        public Chapter? ResultChapter { get; set; }

        // ── Timestamps ───────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClaimedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
