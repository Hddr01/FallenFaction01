// DTOs/Comment/CommentReactionRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Comment
{
    public class CommentReactionRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "CommentId must be a positive integer.")]
        public int CommentId { get; set; }

        public bool IsLike { get; set; } // true = like, false = dislike
    }
}