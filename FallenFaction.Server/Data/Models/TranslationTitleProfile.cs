using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    /// <summary>
    /// Per-title translation settings sent to workers with every job.
    /// </summary>
    public class TranslationTitleProfile
    {
        [Key]
        public int TitleId { get; set; }

        [ForeignKey("TitleId")]
        public Title Title { get; set; } = null!;

        /// <summary>zh | ja | ko | other ISO code. Drives validators and prompt wording.</summary>
        [Required, StringLength(10)]
        public string SourceLanguage { get; set; } = "zh";

        /// <summary>Free-form instructions: tone, honorific policy, tense, POV quirks…</summary>
        [StringLength(4000)]
        public string? StyleGuide { get; set; }

        /// <summary>Series synopsis so the model understands the setting from chapter 1.</summary>
        [StringLength(4000)]
        public string? Synopsis { get; set; }

        /// <summary>When true every chapter waits in NeedsReview, even if QA passes.</summary>
        public bool RequireReview { get; set; } = false;

        /// <summary>Chapters scoring below this (1–10) go to NeedsReview instead of publishing.</summary>
        [Column(TypeName = "decimal(4,2)")]
        public decimal MinQaScore { get; set; } = 7m;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
