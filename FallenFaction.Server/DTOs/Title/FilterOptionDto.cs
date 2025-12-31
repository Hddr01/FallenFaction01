namespace FallenFaction.Server.DTOs.Title
{
    public class FilterOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; } // Number of titles with this option
    }
}
