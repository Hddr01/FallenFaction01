// DTOs/Comment/CommentReactionRequestDto.cs
namespace FallenFaction.Server.DTOs.Comment
{
    public class CommentReactionRequestDto
    {
        public int CommentId { get; set; }
        public bool IsLike { get; set; } // true = like, false = dislike
    }
}