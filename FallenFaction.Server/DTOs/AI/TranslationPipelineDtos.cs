using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.AI
{
    // ── Worker ↔ server contract ──────────────────────────────────────────────

    public class GlossaryTermDto
    {
        [Required, StringLength(200)]
        public string SourceTerm { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Translation { get; set; } = string.Empty;

        [StringLength(30)]
        public string Category { get; set; } = "term";

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsLocked { get; set; }
    }

    public class PreviousChapterSummaryDto
    {
        public int VolumeNumber { get; set; }
        public int ChapterNumber { get; set; }
        public string? Title { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>Everything a worker needs to translate one chapter — no further calls required.</summary>
    public class ClaimedJobDto
    {
        public int JobId { get; set; }
        public int TitleId { get; set; }
        public string TitleName { get; set; } = string.Empty;
        public string? OriginalTitle { get; set; }
        public int VolumeNumber { get; set; }
        public int ChapterNumber { get; set; }
        public string? SourceChapterTitle { get; set; }
        public string SourceText { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = "zh";
        public string TargetLanguage { get; set; } = "en";
        public string? StyleGuide { get; set; }
        public string? Synopsis { get; set; }
        public DateTime LeaseExpiresAt { get; set; }
        public int Attempt { get; set; }

        /// <summary>Glossary entries whose source term occurs in this chapter (exact match).</summary>
        public List<GlossaryTermDto> Glossary { get; set; } = new();

        /// <summary>Summaries of the most recent preceding chapters, oldest first.</summary>
        public List<PreviousChapterSummaryDto> PreviousChapters { get; set; } = new();
    }

    public class CompleteJobDto
    {
        [Required, StringLength(255)]
        public string TranslatedTitle { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Summary { get; set; }

        public List<GlossaryTermDto> NewGlossaryTerms { get; set; } = new();

        /// <summary>Final review score, 1–10.</summary>
        [Range(0, 10)]
        public decimal? QaScore { get; set; }

        /// <summary>Worker-side hard failure of a validator (e.g. untranslated text left).</summary>
        public bool QaFlagged { get; set; }

        /// <summary>Opaque JSON blob with the review issues + validator output, kept for admins.</summary>
        public string? QaReport { get; set; }

        [StringLength(200)]
        public string? ModelInfo { get; set; }
    }

    public class FailJobDto
    {
        [StringLength(2000)]
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// False for problems that aren't the job's fault (usage limit hit, worker shutting down):
        /// the job goes straight back to the queue without burning an attempt.
        /// </summary>
        public bool CountAttempt { get; set; } = true;
    }

    public class CompleteJobResultDto
    {
        public int JobId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ChapterId { get; set; }
        public int GlossaryTermsAdded { get; set; }
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    public class CreateWorkerDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
    }

    public class WorkerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KeyPrefix { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }

    public class CreatedWorkerDto : WorkerDto
    {
        /// <summary>Shown exactly once. Only its hash is stored.</summary>
        public string ApiKey { get; set; } = string.Empty;
    }

    public class EnqueueChapterDto
    {
        [Range(0, 10000)]
        public int VolumeNumber { get; set; } = 1;

        [Range(0, 100000)]
        public int ChapterNumber { get; set; }

        [StringLength(500)]
        public string? SourceChapterTitle { get; set; }

        [Required]
        public string SourceText { get; set; } = string.Empty;
    }

    public class EnqueueJobsDto
    {
        [Required, MinLength(1), MaxLength(200)]
        public List<EnqueueChapterDto> Chapters { get; set; } = new();
    }

    public class EnqueueResultDto
    {
        public int Enqueued { get; set; }
        public int SkippedExisting { get; set; }
        public List<int> JobIds { get; set; } = new();
    }

    public class TranslationProfileDto
    {
        [Required, StringLength(10)]
        public string SourceLanguage { get; set; } = "zh";

        [StringLength(10)]
        public string TargetLanguage { get; set; } = "en";

        [StringLength(4000)]
        public string? StyleGuide { get; set; }

        [StringLength(4000)]
        public string? Synopsis { get; set; }

        public bool RequireReview { get; set; }

        [Range(0, 10)]
        public decimal MinQaScore { get; set; } = 7m;
    }

    public class GlossaryEntryDto : GlossaryTermDto
    {
        public int Id { get; set; }
        public int? FirstSeenChapter { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TranslationJobListItemDto
    {
        public int Id { get; set; }
        public int TitleId { get; set; }
        public int VolumeNumber { get; set; }
        public int ChapterNumber { get; set; }
        public string? SourceChapterTitle { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public string? LastError { get; set; }
        public string? WorkerName { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
        public decimal? QaScore { get; set; }
        public string? TranslatedTitle { get; set; }
        public int? ResultChapterId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class TranslationJobDetailDto : TranslationJobListItemDto
    {
        public string SourceText { get; set; } = string.Empty;
        public string? TranslatedContent { get; set; }
        public string? Summary { get; set; }
        public string? QaReport { get; set; }
        public string? ModelInfo { get; set; }
    }

    public class TitlePipelineStatusDto
    {
        public int TitleId { get; set; }
        public string TitleName { get; set; } = string.Empty;
        public Dictionary<string, int> Counts { get; set; } = new();

        /// <summary>Earliest Failed job — while it exists, later chapters of this title won't be claimed.</summary>
        public TranslationJobListItemDto? BlockedBy { get; set; }
    }
}
