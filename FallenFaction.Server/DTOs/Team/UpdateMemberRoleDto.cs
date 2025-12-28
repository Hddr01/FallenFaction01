// Replace your existing DTOs/Team/UpdateMemberRoleDto.cs with this:
using FallenFaction.Server.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Team
{
    public class UpdateMemberRoleDto
    {
        [Required]
        [Range(0, 2, ErrorMessage = "Role must be 0 (Admin), 1 (Member), or 2 (Viewer)")]
        public TeamRole Role { get; set; }

    }
}