namespace FallenFaction.Server.DTOs.User
{
    public class PublicUserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        /// <summary>Display name: ProfileName if set, otherwise UserName.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Unique @handle (UserName).</summary>
        public string UserName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string? Banner { get; set; }
        public string? Bio { get; set; }
        public int Level { get; set; }
        public int XpPoints { get; set; }
        public bool IsOnline { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsVerified { get; set; }
    }
}
