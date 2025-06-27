// DTOs/Artist/UpdateArtistDto.cs
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Artist
{
    public class UpdateArtistDto
    {
        [Required(ErrorMessage = "Artist name is required")]
        [StringLength(200, ErrorMessage = "Artist name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Alternative names cannot exceed 300 characters")]
        public string OtherName { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;
    }
}