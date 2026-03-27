// Data/Models/UserTrustRecord.cs
namespace FallenFaction.Server.Data.Models
{
    /// <summary>
    /// Tracks per-user, per-action trust level.
    /// A user becomes "trusted" for a given action after an admin has approved
    /// 5 of their submissions for that action type without a single rejection in between.
    /// Once trusted, the system auto-approves future submissions (subject to system checks).
    /// </summary>
    public class UserTrustRecord
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual AppUser User { get; set; } = null!;

        /// <summary>
        /// The action this trust record covers.
        /// </summary>
        public TrustActionType ActionType { get; set; }

        /// <summary>
        /// Total number of admin-approved submissions for this action.
        /// Resets to 0 on rejection so users must rebuild trust.
        /// </summary>
        public int AdminApprovedCount { get; set; } = 0;

        /// <summary>
        /// Whether the user is currently trusted for this action.
        /// Set to true when AdminApprovedCount reaches the threshold.
        /// Revoked if a submission is rejected.
        /// </summary>
        public bool IsTrusted { get; set; } = false;

        /// <summary>
        /// When the user first became trusted for this action.
        /// </summary>
        public DateTime? TrustedAt { get; set; }

        /// <summary>
        /// When this record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum TrustActionType
    {
        AddTitle = 0,
        AddChapter = 1,
        EditTitle = 2,
        EditChapter = 3
    }
}