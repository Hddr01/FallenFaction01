using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(30, ErrorMessage = "Username must be between {2} and {1} characters.", MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, hyphens, and underscores.")]
        public required string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public required string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public required string ConfirmPassword { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}