using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    /// <summary>
    /// Tracks which users have voted on which novel requests.
    /// Unique index on (RequestId, UserId) prevents double-voting.
    /// Eligibility: UserLevel >= 2.
    /// </summary>
    public class TranslationRequestVote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequestId { get; set; }

        [ForeignKey("RequestId")]
        public TranslationRequest Request { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public AppUser User { get; set; } = null!;

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}
