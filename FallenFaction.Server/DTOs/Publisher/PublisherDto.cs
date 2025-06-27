// DTOs/Publisher/PublisherDto.cs
namespace FallenFaction.Server.DTOs.Publisher
{
    public class PublisherDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TitleCount { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}