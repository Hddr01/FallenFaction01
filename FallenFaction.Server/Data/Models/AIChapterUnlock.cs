using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    /// <summary>
    /// Records each AI chapter unlock event. Once a chapter is unlocked
    /// by anyone, it becomes permanently free for all users.
    /// Cost formula: (CharacterCount + 500) × 0.0012, minimum 1 ticket.
    /// </summary>
    public class AIChapterUnlock
    {
        [Key]
        public int Id { get; set; }

        // ── What was unlocked ────────────────────────────────────────────────
        [Required]
        public int ChapterId { get; set; }

        [ForeignKey("ChapterId")]
        public Chapter Chapter { get; set; } = null!;

        [Required]
        public int TitleId { get; set; }

        [ForeignKey("TitleId")]
        public Title Title { get; set; } = null!;

        // ── Who unlocked it ──────────────────────────────────────────────────
        [Required]
        public string UnlockedByUserId { get; set; } = string.Empty;

        [ForeignKey("UnlockedByUserId")]
        public AppUser UnlockedByUser { get; set; } = null!;

        // ── Cost tracking ────────────────────────────────────────────────────
        /// <summary>Tickets spent to unlock this chapter.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TicketCost { get; set; }

        /// <summary>Which ticket type was used. Currently always Silver.</summary>
        public TicketType TicketTypeUsed { get; set; }

        /// <summary>Chapter character count at time of unlock (for audit).</summary>
        public int CharacterCount { get; set; }

        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    }
}
