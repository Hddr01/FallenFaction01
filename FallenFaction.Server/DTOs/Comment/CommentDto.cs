namespace FallenFaction.Server.DTOs.Comment
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime PostedDate { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
        public bool CurrentUserLiked { get; set; }
        public bool CurrentUserDisliked { get; set; }
        public int? ParentCommentId { get; set; }
        public List<CommentDto> Replies { get; set; } = new List<CommentDto>();

        // Target information
        public int? TitleId { get; set; }
        public int? ChapterId { get; set; }

        // ✅ NEW: Soft delete fields
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserName { get; set; }
        public string? DeletionReason { get; set; }
    }
}