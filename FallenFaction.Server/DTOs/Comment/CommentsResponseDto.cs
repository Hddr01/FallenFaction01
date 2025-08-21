// DTOs/Comment/CommentsResponseDto.cs
namespace FallenFaction.Server.DTOs.Comment
{
    public class CommentsResponseDto
    {
        public List<CommentDto> Comments { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }
}