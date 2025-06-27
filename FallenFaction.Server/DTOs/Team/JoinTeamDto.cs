// DTOs/Team/JoinTeamDto.cs
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Team
{
    public class JoinTeamDto
    {
        [Required]
        public int TeamId { get; set; }
    }
}