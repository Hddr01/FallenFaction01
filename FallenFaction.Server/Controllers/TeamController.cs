// Controllers/TeamController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Team;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TeamController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/team
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeamListDto>>> GetTeams()
        {
            var teams = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Titles)
                .Select(t => new TeamListDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    CreatorName = _context.Users.Where(u => u.Id == t.CreatorId).Select(u => u.UserName).FirstOrDefault(),
                    MemberCount = t.Members.Count,
                    TitleCount = t.Titles.Count,
                    CreatedDate = DateTime.UtcNow // You might want to add a CreatedDate property to Team model
                })
                .ToListAsync();

            return Ok(teams);
        }

        // GET: api/team/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TeamDto>> GetTeam(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.UserTeamRoles)
                    .ThenInclude(utr => utr.AppUser)
                .Include(t => t.Titles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            var teamDto = new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                CreatorId = team.CreatorId,
                CreatorName = (await _userManager.FindByIdAsync(team.CreatorId))?.UserName,
                MemberCount = team.Members.Count,
                TitleCount = team.Titles.Count,
                Members = team.UserTeamRoles.Select(utr => new TeamMemberDto
                {
                    UserId = utr.AppUserId,
                    UserName = utr.AppUser.UserName,
                    Email = utr.AppUser.Email,
                    ProfilePicturePath = utr.AppUser.ProfilePicturePath,
                    Role = utr.Role,
                    JoinedDate = DateTime.UtcNow, // You might want to add this to UserTeamRole
                    IsOnline = utr.AppUser.IsOnline
                }).ToList()
            };

            return Ok(teamDto);
        }

        // Controllers/TeamController.cs - Updated CreateTeam method
        [HttpPost]
        public async Task<ActionResult<TeamDto>> CreateTeam(CreateTeamDto createTeamDto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var team = new Team
            {
                Name = createTeamDto.Name,
                Description = createTeamDto.Description,
                CreatorId = currentUser.Id
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Add creator as admin member - THIS IS CRUCIAL!
            var userTeamRole = new UserTeamRole
            {
                AppUserId = currentUser.Id,
                TeamId = team.Id,
                Role = TeamRole.Admin
            };

            _context.UserTeamRoles.Add(userTeamRole);

            // Also add to Members collection for proper counting
            team.Members.Add(currentUser);

            await _context.SaveChangesAsync();

            // Return the created team with the creator as a member
            var teamDto = new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                CreatorId = team.CreatorId,
                CreatorName = currentUser.UserName,
                MemberCount = 1,
                TitleCount = 0,
                Members = new List<TeamMemberDto>
        {
            new TeamMemberDto
            {
                UserId = currentUser.Id,
                UserName = currentUser.UserName,
                Email = currentUser.Email,
                ProfilePicturePath = currentUser.ProfilePicturePath,
                Role = TeamRole.Admin,
                JoinedDate = DateTime.UtcNow,
                IsOnline = currentUser.IsOnline
            }
        }
            };

            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, teamDto);
        }

        // Also add this alias endpoint for NavBar compatibility
        [HttpGet("~/api/Teams/GetUserTeams")]
        public async Task<ActionResult<IEnumerable<TeamListDto>>> GetUserTeams()
        {
            return await GetMyTeams();
        }

        // PUT: api/team/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(int id, UpdateTeamDto updateTeamDto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            // Check if user is admin or creator
            var userRole = await _context.UserTeamRoles
                .FirstOrDefaultAsync(utr => utr.AppUserId == currentUser.Id && utr.TeamId == id);

            if (team.CreatorId != currentUser.Id && (userRole == null || userRole.Role != TeamRole.Admin))
            {
                return Forbid();
            }

            team.Name = updateTeamDto.Name;
            team.Description = updateTeamDto.Description;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/team/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            // Only creator can delete team
            if (team.CreatorId != currentUser.Id)
            {
                return Forbid();
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/team/{id}/join
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinTeam(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            // Check if user is already a member
            var existingRole = await _context.UserTeamRoles
                .FirstOrDefaultAsync(utr => utr.AppUserId == currentUser.Id && utr.TeamId == id);

            if (existingRole != null)
            {
                return BadRequest("You are already a member of this team.");
            }

            var userTeamRole = new UserTeamRole
            {
                AppUserId = currentUser.Id,
                TeamId = id,
                Role = TeamRole.Member
            };

            _context.UserTeamRoles.Add(userTeamRole);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/team/{id}/leave
        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> LeaveTeam(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            // Creator cannot leave their own team
            if (team.CreatorId == currentUser.Id)
            {
                return BadRequest("Team creator cannot leave the team. Transfer ownership or delete the team instead.");
            }

            var userTeamRole = await _context.UserTeamRoles
                .FirstOrDefaultAsync(utr => utr.AppUserId == currentUser.Id && utr.TeamId == id);

            if (userTeamRole == null)
            {
                return BadRequest("You are not a member of this team.");
            }

            _context.UserTeamRoles.Remove(userTeamRole);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // PUT: api/team/{id}/members/{userId}/role
        [HttpPut("{id}/members/{userId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int id, string userId, UpdateMemberRoleDto updateRoleDto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            // Check if current user is admin or creator
            var currentUserRole = await _context.UserTeamRoles
                .FirstOrDefaultAsync(utr => utr.AppUserId == currentUser.Id && utr.TeamId == id);

            if (team.CreatorId != currentUser.Id && (currentUserRole == null || currentUserRole.Role != TeamRole.Admin))
            {
                return Forbid();
            }

            var targetUserRole = await _context.UserTeamRoles
                .FirstOrDefaultAsync(utr => utr.AppUserId == userId && utr.TeamId == id);

            if (targetUserRole == null)
            {
                return NotFound("User is not a member of this team.");
            }

            // Cannot change creator's role
            if (team.CreatorId == userId)
            {
                return BadRequest("Cannot change team creator's role.");
            }

            targetUserRole.Role = updateRoleDto.Role;
            await _context.SaveChangesAsync();

            return Ok();
        }


        // GET: api/team/my-teams
        [HttpGet("my-teams")]
        public async Task<ActionResult<IEnumerable<TeamListDto>>> GetMyTeams()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var teams = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == currentUser.Id)
                .Include(utr => utr.Team)
                    .ThenInclude(t => t.Members)
                .Include(utr => utr.Team)
                    .ThenInclude(t => t.Titles)
                .Select(utr => new TeamListDto
                {
                    Id = utr.Team.Id,
                    Name = utr.Team.Name,
                    Description = utr.Team.Description,
                    CreatorName = _context.Users.Where(u => u.Id == utr.Team.CreatorId).Select(u => u.UserName).FirstOrDefault(),
                    MemberCount = utr.Team.Members.Count,
                    TitleCount = utr.Team.Titles.Count,
                    CreatedDate = DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(teams);
        }
    }
}