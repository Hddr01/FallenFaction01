// DTOs/Comment/GetCommentsRequestDto.cs
namespace FallenFaction.Server.DTOs.Comment
{
    public class GetCommentsRequestDto
    {
        public int TargetId { get; set; }
        public int TargetType { get; set; } // 1 = Title, 2 = Chapter
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "newest"; // newest, oldest, likes
    }
}