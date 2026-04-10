using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Auth
{
    public class AcceptTermsDto
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public required string Email { get; set; }

        [Required]
        [StringLength(100)]
        public required string Password { get; set; }
    }
}
