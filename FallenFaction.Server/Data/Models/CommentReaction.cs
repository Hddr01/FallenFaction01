using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace FallenFaction.Server.Data.Models
{
    public class CommentReaction
    {
        [Key]
        public int Id { get; set; }

        // User who created the reaction
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        // Comment being reacted to
        public int CommentId { get; set; }
        [ForeignKey("CommentId")]
        public Comment Comment { get; set; }

        // Type of reaction
        public bool IsLike { get; set; } // true = like, false = dislike

        // Timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
