using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Author
{
    public class CreateAuthorDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Other name cannot exceed 300 characters")]
        public string OtherName { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;
    }
}
