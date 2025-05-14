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

        // Reference to ChapterImage (may be null if comment is on title or chapter)
        public int? ChapterImageId { get; set; }
        [ForeignKey("ChapterImageId")]
        public ChapterImage ChapterImage { get; set; }

        // Support for nested comments/replies
        public int? ParentCommentId { get; set; }
        [ForeignKey("ParentCommentId")]
        public Comment ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        // User reactions (likes/dislikes)
        public ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();
    }
}
