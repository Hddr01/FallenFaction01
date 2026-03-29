using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public enum TicketType
    {
        Gold = 1,
        Silver = 2
    }

    public enum TicketTransactionType
    {
        // ── Earning ──────────────────────────────────────────────────────────
        PatreonGrant = 1,       // Monthly Patreon tier → Gold
        AdminGrant = 2,         // Manual admin grant → Gold or Silver
        Contribution = 3,       // API key / community contribution → Silver
        Refund = 4,             // Admin refund of a prior spend

        // ── Spending ─────────────────────────────────────────────────────────
        ChapterUnlock = 10,     // Spent to unlock an AI chapter
        NovelRelease = 11,      // Spent to release/fast-release a novel

        // ── System ───────────────────────────────────────────────────────────
        Expiry = 20,            // Silver tickets expired after 3 months
        Adjustment = 21         // Admin manual balance correction
    }

    /// <summary>
    /// Immutable ledger row. Every ticket balance change creates one of these.
    /// Positive Amount = earning, negative Amount = spending.
    /// </summary>
    public class TicketTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public AppUser User { get; set; } = null!;

        public TicketType TicketType { get; set; }

        public TicketTransactionType TransactionType { get; set; }

        /// <summary>Positive = credit, negative = debit.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>Balance AFTER this transaction (for audit trail).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        /// <summary>Human-readable description, e.g. "Unlocked Chapter 51 of Solo Leveling".</summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Optional link to the related entity (title, chapter, etc.).</summary>
        public int? RelatedTitleId { get; set; }
        public int? RelatedChapterId { get; set; }
        public int? RelatedRequestId { get; set; }

        /// <summary>For Silver tickets: when this batch expires.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>For Patreon grants: the Patreon tier name at time of grant.</summary>
        [StringLength(100)]
        public string? PatreonTierName { get; set; }

        /// <summary>Admin who performed the grant/adjustment, if applicable.</summary>
        public string? PerformedByUserId { get; set; }

        [ForeignKey("PerformedByUserId")]
        public AppUser? PerformedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
