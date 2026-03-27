using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace FallenFaction.Server.Data.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime PostedDate { get; set; }

        // User relationship
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        // Likes and dislikes
        public int LikesCount { get; set; } = 0;
        public int DislikesCount { get; set; } = 0;

        // ✅ NEW: Soft delete fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
        [ForeignKey("DeletedByUserId")]
        public AppUser? DeletedByUser { get; set; }
        public string? DeletionReason { get; set; } // Optional reason for deletion

        // What the comment is attached to - we'll use foreign keys with shadow properties
        // to determine what this comment is for (title, chapter, or image)

        // Reference to Title (may be null if comment is on chapter or image)
        public int? TitleId { get; set; }
        [ForeignKey("TitleId")]
        public Title Title { get; set; }

        // Reference to Chapter (may be null if comment is on title or image)
        public int? ChapterId { get; set; }
        [ForeignKey("ChapterId")]
        public Chapter Chapter { get; set; }

        // Support for nested comments/replies
        public int? ParentCommentId { get; set; }
        [ForeignKey("ParentCommentId")]
        public Comment ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        // User reactions (likes/dislikes)
        public ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();

        // ✅ Helper method to get display content
        [NotMapped]
        public string DisplayContent
        {
            get
            {
                if (IsDeleted)
                {
                    return "[This comment has been deleted]";
                }
                return Content;
            }
        }

        // ✅ Helper method to check if user can see deleted content
        public bool CanUserSeeDeletedContent(string currentUserId, bool isAdmin = false)
        {
            if (!IsDeleted) return true;

            // Admins can see deleted content
            if (isAdmin) return true;

            // Original author can see their deleted content
            if (UserId == currentUserId) return true;

            return false;
        }

        // ✅ Soft delete method
        public void SoftDelete(string deletedByUserId, string reason = null)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
            DeletionReason = reason;
        }

        // ✅ Restore method
        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedByUserId = null;
            DeletionReason = null;
        }
    }
}