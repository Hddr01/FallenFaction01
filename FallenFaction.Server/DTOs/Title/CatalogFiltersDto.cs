using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Title
{
    public class CatalogFiltersDto
    {
        public string? Search { get; set; }
        public MangaType? Type { get; set; }
        public string? Status { get; set; }
        public string? TranslationStatus { get; set; }
        public int? AgeRestriction { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Formats { get; set; } = new List<string>();
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public string SortBy { get; set; } = "updated";
        public string SortOrder { get; set; } = "desc";
    }
}
