using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Auth
{
    public class AcceptTermsDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
