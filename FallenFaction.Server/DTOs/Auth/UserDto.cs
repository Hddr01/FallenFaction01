using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Auth
{
    public class UserDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicturePath { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastActive { get; set; }
        public bool IsOnline { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public bool IsBannedFromComments { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}