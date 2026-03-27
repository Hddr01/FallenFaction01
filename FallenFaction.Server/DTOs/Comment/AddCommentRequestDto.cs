namespace FallenFaction.Server.DTOs.Comment
{
    public class AddCommentRequestDto
    {
        public string Content { get; set; } = string.Empty;
        public int TargetType { get; set; } // 1 = Title, 2 = Chapter
        public int TargetId { get; set; }
        public int? ParentCommentId { get; set; }
    }
}