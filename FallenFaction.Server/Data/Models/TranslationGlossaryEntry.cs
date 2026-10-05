using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    /// <summary>
    /// Per-title term memory: names, places, cultivation ranks, techniques…
    /// Keeps a term translated the same way across hundreds of chapters.
    /// Worker-proposed entries never overwrite existing ones; admin-locked entries are authoritative.
    /// </summary>
    public class TranslationGlossaryEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TitleId { get; set; }

        [ForeignKey("TitleId")]
        public Title Title { get; set; } = null!;

        [Required, StringLength(200)]
        public string SourceTerm { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Translation { get; set; } = string.Empty;

        /// <summary>character | place | organization | technique | item | rank | term | other</summary>
        [StringLength(30)]
        public string Category { get; set; } = "term";

        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>Set by an admin edit — workers must follow it exactly.</summary>
        public bool IsLocked { get; set; } = false;

        /// <summary>Chapter number where the term was first seen (worker-proposed entries).</summary>
        public int? FirstSeenChapter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
