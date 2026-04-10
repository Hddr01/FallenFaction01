using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Comment
{
    public class AddCommentRequestDto
    {
        [Required]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 2000 characters.")]
        public string Content { get; set; } = string.Empty;

        [Range(1, 3, ErrorMessage = "TargetType must be 1 (Title), 2 (Chapter), or 3 (Profile).")]
        public int TargetType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TargetId must be a positive integer.")]
        public int TargetId { get; set; }

        public int? ParentCommentId { get; set; }
    }
}