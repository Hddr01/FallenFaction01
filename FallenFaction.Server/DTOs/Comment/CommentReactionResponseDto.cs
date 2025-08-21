// DTOs/Comment/CommentReactionResponseDto.cs
namespace FallenFaction.Server.DTOs.Comment
{
    public class CommentReactionResponseDto
    {
        public int CommentId { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
        public bool UserLiked { get; set; }
        public bool UserDisliked { get; set; }
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
    }
}