// DTOs/Comment/CommentStatsDto.cs
namespace FallenFaction.Server.DTOs.Comment
{
    public class CommentStatsDto
    {
        public int TotalComments { get; set; }
        public int TopLevelComments { get; set; }
        public int Replies { get; set; }
        public DateTime? LastCommentDate { get; set; }
        public bool CommentsEnabled { get; set; }
    }
}
