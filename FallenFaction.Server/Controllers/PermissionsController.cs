using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Services;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissions;
        private readonly UserManager<AppUser> _userManager;

        public PermissionsController(IPermissionService permissions, UserManager<AppUser> userManager)
        {
            _permissions = permissions;
            _userManager = userManager;
        }

        // GET: api/permissions/me
        // Returns the per-user, role-based + team-based permission summary the
        // frontend needs to gate UI without re-asking the server for each
        // button. Lives behind an authenticated route — anonymous users have
        // no permissions, so usePermissions() on the client treats absence as
        // empty.
        [HttpGet("me")]
        public async Task<ActionResult<UserPermissionsDto>> GetMyPermissions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var addTitleTeamIds = await _permissions.GetTeamIdsWithPermissionAsync(userId, Permissions.CanAddTitle);
            var editTitleTeamIds = await _permissions.GetTeamIdsWithPermissionAsync(userId, Permissions.CanEditTitle);

            return Ok(new UserPermissionsDto
            {
                IsAdmin = roles.Contains("Admin"),
                IsModerator = roles.Contains("Moderator") || roles.Contains("Admin"),
                CanAddTitleTeamIds = addTitleTeamIds,
                CanEditTitleTeamIds = editTitleTeamIds
            });
        }
    }

    public sealed class UserPermissionsDto
    {
        public bool IsAdmin { get; set; }
        public bool IsModerator { get; set; }
        public IReadOnlyList<int> CanAddTitleTeamIds { get; set; } = [];
        public IReadOnlyList<int> CanEditTitleTeamIds { get; set; } = [];
    }
}
