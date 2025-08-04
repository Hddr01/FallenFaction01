using Microsoft.AspNetCore.Mvc;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminTeamController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminTeamController> _logger;
        private readonly UserManager<AppUser> _userManager;

        public AdminTeamController(
            ApplicationDbContext context,
            ILogger<AdminTeamController> logger,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Get all teams with search functionality (Admin only)
        /// GET: api/AdminTeam
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllTeams(string? searchString = null)
        {
            try
            {
                var query = _context.Teams
                    .Include(t => t.Members)
                    .Include(t => t.UserTeamRoles)
                        .ThenInclude(utr => utr.AppUser)
                    .Include(t => t.Titles)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.ToLower();
                    query = query.Where(t =>
                        t.Name.ToLower().Contains(searchString) ||
                        t.Description.ToLower().Contains(searchString));
                }

                var teams = await query
                    .Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        description = t.Description,
                        creatorId = t.CreatorId,
                        creatorName = _context.Users.Where(u => u.Id == t.CreatorId).Select(u => u.UserName).FirstOrDefault(),
                        memberCount = t.Members.Count,
                        titleCount = t.Titles.Count,
                        createdDate = DateTime.UtcNow, // Add CreatedDate to Team model if needed
                        members = t.UserTeamRoles.Select(utr => new
                        {
                            userId = utr.AppUserId,
                            userName = utr.AppUser.UserName,
                            email = utr.AppUser.Email,
                            role = utr.Role,
                            isOnline = utr.AppUser.IsOnline
                        })
                    })
                    .ToListAsync();

                return Ok(teams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching teams for admin");
                return StatusCode(500, new { message = "Error fetching teams", error = ex.Message });
            }
        }

        /// <summary>
        /// Search teams by string
        /// GET: api/AdminTeam/SearchTeam?searchString={searchString}
        /// </summary>
        [HttpGet("SearchTeam")]
        public async Task<ActionResult<IEnumerable<object>>> SearchTeam(string searchString)
        {
            return await GetAllTeams(searchString);
        }

        /// <summary>
        /// Get team details by ID (Admin only)
        /// GET: api/AdminTeam/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTeamById(int id)
        {
            try
            {
                var team = await _context.Teams
                    .Include(t => t.Members)
                    .Include(t => t.UserTeamRoles)
                        .ThenInclude(utr => utr.AppUser)
                    .Include(t => t.Titles)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (team == null)
                {
                    return NotFound(new { message = "Team not found" });
                }

                var creator = await _userManager.FindByIdAsync(team.CreatorId);

                var result = new
                {
                    id = team.Id,
                    name = team.Name,
                    description = team.Description,
                    creatorId = team.CreatorId,
                    creatorName = creator?.UserName,
                    memberCount = team.Members.Count,
                    titleCount = team.Titles.Count,
                    members = team.UserTeamRoles.Select(utr => new
                    {
                        userId = utr.AppUserId,
                        userName = utr.AppUser.UserName,
                        email = utr.AppUser.Email,
                        profilePicturePath = utr.AppUser.ProfilePicturePath,
                        role = utr.Role,
                        joinedDate = DateTime.UtcNow, // Add JoinedDate to UserTeamRole if needed
                        isOnline = utr.AppUser.IsOnline
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching team details for ID: {Id}", id);
                return StatusCode(500, new { message = "Error fetching team details", error = ex.Message });
            }
        }

        /// <summary>
        /// Update team (Admin can edit any team)
        /// PUT: api/AdminTeam/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
        {
            try
            {
                var team = await _context.Teams.FindAsync(id);
                if (team == null)
                {
                    return NotFound(new { message = "Team not found" });
                }

                team.Name = request.Name;
                team.Description = request.Description;

                _context.Teams.Update(team);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Team {TeamId} updated by admin", id);
                return Ok(new { message = "Team updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team {TeamId}", id);
                return StatusCode(500, new { message = "Error updating team", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete team (Admin can delete any team)
        /// DELETE: api/AdminTeam/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteTeam(int id)
        {
            try
            {
                var team = await _context.Teams
                    .Include(t => t.UserTeamRoles)
                    .Include(t => t.Members)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (team == null)
                {
                    return NotFound(new { message = "Team not found" });
                }

                _context.Teams.Remove(team);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Team {TeamId} deleted by admin", id);
                return Ok(new { message = "Team deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team {TeamId}", id);
                return StatusCode(500, new { message = "Error deleting team", error = ex.Message });
            }
        }

        /// <summary>
        /// Remove member from team (Admin only)
        /// DELETE: api/AdminTeam/{teamId}/members/{userId}
        /// </summary>
        [HttpDelete("{teamId}/members/{userId}")]
        public async Task<ActionResult<object>> RemoveMember(int teamId, string userId)
        {
            try
            {
                var team = await _context.Teams.FindAsync(teamId);
                if (team == null)
                {
                    return NotFound(new { message = "Team not found" });
                }

                // Don't allow removing the team creator
                if (team.CreatorId == userId)
                {
                    return BadRequest(new { message = "Cannot remove team creator from team" });
                }

                var userTeamRole = await _context.UserTeamRoles
                    .FirstOrDefaultAsync(utr => utr.AppUserId == userId && utr.TeamId == teamId);

                if (userTeamRole == null)
                {
                    return NotFound(new { message = "User is not a member of this team" });
                }

                _context.UserTeamRoles.Remove(userTeamRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} removed from team {TeamId} by admin", userId, teamId);
                return Ok(new { message = "Member removed successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member {UserId} from team {TeamId}", userId, teamId);
                return StatusCode(500, new { message = "Error removing member", error = ex.Message });
            }
        }

        /// <summary>
        /// Update member role (Admin only)
        /// PUT: api/AdminTeam/{teamId}/members/{userId}/role
        /// </summary>
        [HttpPut("{teamId}/members/{userId}/role")]
        public async Task<ActionResult<object>> UpdateMemberRole(int teamId, string userId, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var team = await _context.Teams.FindAsync(teamId);
                if (team == null)
                {
                    return NotFound(new { message = "Team not found" });
                }

                // Don't allow changing creator's role
                if (team.CreatorId == userId)
                {
                    return BadRequest(new { message = "Cannot change team creator's role" });
                }

                var userTeamRole = await _context.UserTeamRoles
                    .FirstOrDefaultAsync(utr => utr.AppUserId == userId && utr.TeamId == teamId);

                if (userTeamRole == null)
                {
                    return NotFound(new { message = "User is not a member of this team" });
                }

                userTeamRole.Role = request.Role;
                _context.UserTeamRoles.Update(userTeamRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} role updated to {Role} in team {TeamId} by admin", userId, request.Role, teamId);
                return Ok(new { message = "Member role updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user {UserId} in team {TeamId}", userId, teamId);
                return StatusCode(500, new { message = "Error updating member role", error = ex.Message });
            }
        }

        /// <summary>
        /// Get team statistics (Admin only)
        /// GET: api/AdminTeam/Statistics
        /// </summary>
        [HttpGet("Statistics")]
        public async Task<ActionResult<object>> GetTeamStatistics()
        {
            try
            {
                var totalTeams = await _context.Teams.CountAsync();
                var totalMembers = await _context.UserTeamRoles.CountAsync();
                var averageMembersPerTeam = totalTeams > 0 ? (double)totalMembers / totalTeams : 0;
                var teamsWithTitles = await _context.Teams.Where(t => t.Titles.Any()).CountAsync();

                var result = new
                {
                    totalTeams = totalTeams,
                    totalMembers = totalMembers,
                    averageMembersPerTeam = Math.Round(averageMembersPerTeam, 2),
                    teamsWithTitles = teamsWithTitles,
                    teamsWithoutTitles = totalTeams - teamsWithTitles
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching team statistics");
                return StatusCode(500, new { message = "Error fetching statistics", error = ex.Message });
            }
        }

        // Request DTOs
        public class UpdateTeamRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class UpdateRoleRequest
        {
            public TeamRole Role { get; set; }
        }
    }
}