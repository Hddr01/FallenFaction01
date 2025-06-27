// DTOs/Artist/ArtistDto.cs
namespace FallenFaction.Server.DTOs.Artist
{
    public class ArtistDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OtherName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TitleCount { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}