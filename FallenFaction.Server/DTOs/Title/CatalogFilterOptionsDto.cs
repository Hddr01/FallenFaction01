namespace FallenFaction.Server.DTOs.Title
{
    public class CatalogFilterOptionsDto
    {
        public List<FilterOptionDto> Authors { get; set; } = new List<FilterOptionDto>();
        public List<FilterOptionDto> Artists { get; set; } = new List<FilterOptionDto>();
        public List<FilterOptionDto> Publishers { get; set; } = new List<FilterOptionDto>();
        public List<FilterOptionDto> Teams { get; set; } = new List<FilterOptionDto>();
        public List<FilterOptionDto> Categories { get; set; } = new List<FilterOptionDto>();
        public List<FilterOptionDto> Tags { get; set; } = new List<FilterOptionDto>();
        public List<FilterOptionDto> Formats { get; set; } = new List<FilterOptionDto>();
    }
}
