// DTOs/Comment/PaginationDto.cs
namespace FallenFaction.Server.DTOs.Comment
{
    public class PaginationDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}