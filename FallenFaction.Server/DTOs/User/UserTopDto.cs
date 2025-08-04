// DTOs/User/UserTopDto.cs
namespace FallenFaction.Server.DTOs.User
{
    public class UserTopDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Score { get; set; } = string.Empty;
    }
}