// DTOs/Team/UpdateMemberRoleDto.cs
using FallenFaction.Server.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Team
{
    public class UpdateMemberRoleDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public TeamRole Role { get; set; }
    }
}