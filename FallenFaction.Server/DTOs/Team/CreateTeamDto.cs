// DTOs/Team/CreateTeamDto.cs
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Team
{
    public class CreateTeamDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Team name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }
    }
}