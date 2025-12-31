namespace FallenFaction.Server.DTOs.Title
{
    public class CatalogResponseDto
    {
        public List<TitleCatalogDto> Items { get; set; } = new List<TitleCatalogDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
