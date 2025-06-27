// DTOs/Publisher/CreatePublisherDto.cs
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Publisher
{
    public class CreatePublisherDto
    {
        [Required(ErrorMessage = "Publisher name is required")]
        [StringLength(200, ErrorMessage = "Publisher name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;
    }
}