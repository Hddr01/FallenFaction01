using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Contact
{
    public class ContactFormDto
    {
        [Required]
        [StringLength(20)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;
    }
}
