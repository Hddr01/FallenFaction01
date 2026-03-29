namespace FallenFaction.Server.DTOs.AI
{
    // ── Wallet ────────────────────────────────────────────────────────────────

    public class WalletDto
    {
        public decimal GoldBalance { get; set; }
        public decimal SilverBalance { get; set; }
        public decimal TotalBalance { get; set; }
        public bool CanVote { get; set; }
        public int UserLevel { get; set; }
        public int XpPoints { get; set; }
    }

    public class TransactionDto
    {
        public int Id { get; set; }
        public string TicketType { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? RelatedTitleId { get; set; }
        public int? RelatedChapterId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? PatreonTierName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Admin grant ───────────────────────────────────────────────────────────

    public class AdminGrantTicketsDto
    {
        public string UserId { get; set; } = string.Empty;
        public string TicketType { get; set; } = "Gold";       // "Gold" | "Silver"
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        /// <summary>Only for Silver: how many months until expiry (default 3).</summary>
        public int? ExpiryMonths { get; set; }
    }

    // ── Chapter unlock ────────────────────────────────────────────────────────

    public class UnlockChapterDto
    {
        public int ChapterId { get; set; }
    }

    public class UnlockChapterResponseDto
    {
        public bool Success { get; set; }
        public decimal TicketsSpent { get; set; }
        public decimal NewGoldBalance { get; set; }
        public decimal NewSilverBalance { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ChapterUnlockCostDto
    {
        public int ChapterId { get; set; }
        public int CharacterCount { get; set; }
        public decimal Cost { get; set; }
        public bool IsAlreadyUnlocked { get; set; }
    }

    // ── Translation requests ──────────────────────────────────────────────────

    public class CreateTranslationRequestDto
    {
        public string SourceUrl { get; set; } = string.Empty;
        public string ProposedTitle { get; set; } = string.Empty;
        public string? OriginalLanguageTitle { get; set; }
        public string? Description { get; set; }
        public string Genres { get; set; } = string.Empty;   // comma-separated
        public string Tags { get; set; } = string.Empty;     // comma-separated
        public string? CoverImageUrl { get; set; }
        public int? EstimatedChapterCount { get; set; }
    }

    public class TranslationRequestDto
    {
        public int Id { get; set; }
        public string RequestedByUserId { get; set; } = string.Empty;
        public string RequestedByUserName { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public string ProposedTitle { get; set; } = string.Empty;
        public string? OriginalLanguageTitle { get; set; }
        public string? Description { get; set; }
        public string Genres { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public int? EstimatedChapterCount { get; set; }
        public int VoteCount { get; set; }
        public bool HasUserVoted { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public int? ReleasedTitleId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
    }

    public class AdminReviewRequestDto
    {
        public int RequestId { get; set; }
        /// <summary>"Approve" | "Reject" | "PreProcessing"</summary>
        public string Action { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public string? AdminNotes { get; set; }
    }

    public class AdminReleaseRequestDto
    {
        public int RequestId { get; set; }
        public int TitleId { get; set; }   // The Title row that was created for this release
    }
}
