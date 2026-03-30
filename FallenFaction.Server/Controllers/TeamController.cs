// Controllers/TeamController.cs - Updated to match existing UserTeamRole model
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.DTOs.Team;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TeamController> _logger;

        public TeamController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<TeamController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: api/team
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetAllTeams()
        {
            var teams = await _context.Teams
                .Include(t => t.UserTeamRoles)
                    .ThenInclude(utr => utr.AppUser)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.CreatorId,
                    t.AvatarImagePath,
                    t.BackgroundImagePath,
                    t.CreatedDate,
                    MemberCount = t.UserTeamRoles.Count,
                    TitleCount = t.Titles.Count
                })
                .ToListAsync();

            return Ok(teams);
        }

        // GET: api/team/TopTeams
        [HttpGet("TopTeams")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TeamTopDto>>> GetTopTeams([FromQuery] int count = 10)
        {
            var teams = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .Include(t => t.Titles)
                    .ThenInclude(title => title.Chapters)
                .OrderByDescending(t => t.Titles.Count)
                .Take(count)
                .Select(t => new TeamTopDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Avatar = t.AvatarImagePath ?? string.Empty,
                    Level = t.UserTeamRoles.Count,
                    Progress = t.Titles.Count,
                    Score = t.Titles.SelectMany(title => title.Chapters).Count().ToString()
                })
                .ToListAsync();

            return Ok(teams);
        }

        // GET: api/team/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTeamById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                    .ThenInclude(utr => utr.AppUser)
                .Include(t => t.Titles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            var userRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            var response = new
            {
                team.Id,
                team.Name,
                team.Description,
                team.CreatorId,
                team.AvatarImagePath,
                team.BackgroundImagePath,
                team.CreatedDate,
                Members = team.UserTeamRoles.Select(utr => new
                {
                    UserId = utr.AppUserId,
                    Username = utr.AppUser.UserName,
                    Role = utr.Role,
                    JoinedDate = utr.AppUser.RegistrationDate // Using registration date as fallback
                }).ToList(),
                Titles = team.Titles.Select(t => new
                {
                    t.Id,
                    t.EnglishTitle,
                    t.CoverImagePath
                }).ToList(),
                UserRole = userRole?.Role,
                IsMember = userRole != null,
                IsCreator = team.CreatorId == userId
            };

            return Ok(response);
        }

        // POST: api/team
        [HttpPost]
        public async Task<ActionResult<Team>> CreateTeam([FromBody] CreateTeamDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var team = new Team
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatorId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Add creator as admin
            var userTeamRole = new UserTeamRole
            {
                AppUserId = userId,
                TeamId = team.Id,
                Role = TeamRole.Admin
            };

            _context.UserTeamRoles.Add(userTeamRole);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeamById), new { id = team.Id }, team);
        }

        // PUT: api/team/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(int id, [FromBody] UpdateTeamDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Check if user is admin
            var userRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            if (userRole?.Role != TeamRole.Admin && team.CreatorId != userId)
            {
                return Forbid();
            }

            team.Name = dto.Name;
            team.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Team updated successfully" });
        }

        // POST: api/team/{id}/upload-avatar
        [HttpPost("{id}/upload-avatar")]
        public async Task<IActionResult> UploadAvatar(int id, IFormFile file)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Check if user is admin
            var userRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            if (userRole?.Role != TeamRole.Admin && team.CreatorId != userId)
            {
                return Forbid();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file type. Only images are allowed." });
            }

            // Validate file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds 5MB limit" });
            }

            try
            {
                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(team.AvatarImagePath))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, team.AvatarImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                // Create directory if not exists
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "teams", "avatars");
                Directory.CreateDirectory(uploadsDir);

                // Generate unique filename
                var fileName = $"team_{id}_avatar_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update database
                team.AvatarImagePath = $"/uploads/teams/avatars/{fileName}";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Avatar uploaded successfully",
                    avatarPath = team.AvatarImagePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for team {TeamId}", id);
                return StatusCode(500, new { message = "Error uploading avatar" });
            }
        }

        // POST: api/team/{id}/upload-background
        [HttpPost("{id}/upload-background")]
        public async Task<IActionResult> UploadBackground(int id, IFormFile file)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Check if user is admin
            var userRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            if (userRole?.Role != TeamRole.Admin && team.CreatorId != userId)
            {
                return Forbid();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file type. Only images are allowed." });
            }

            // Validate file size (10MB max for backgrounds)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds 10MB limit" });
            }

            try
            {
                // Delete old background if exists
                if (!string.IsNullOrEmpty(team.BackgroundImagePath))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, team.BackgroundImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                // Create directory if not exists
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "teams", "backgrounds");
                Directory.CreateDirectory(uploadsDir);

                // Generate unique filename
                var fileName = $"team_{id}_bg_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update database
                team.BackgroundImagePath = $"/uploads/teams/backgrounds/{fileName}";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Background uploaded successfully",
                    backgroundPath = team.BackgroundImagePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading background for team {TeamId}", id);
                return StatusCode(500, new { message = "Error uploading background" });
            }
        }

        // DELETE: api/team/{id}/avatar
        [HttpDelete("{id}/avatar")]
        public async Task<IActionResult> DeleteAvatar(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Check if user is admin
            var userRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            if (userRole?.Role != TeamRole.Admin && team.CreatorId != userId)
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(team.AvatarImagePath))
            {
                return BadRequest(new { message = "No avatar to delete" });
            }

            try
            {
                // Delete file
                var filePath = Path.Combine(_environment.WebRootPath, team.AvatarImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Update database
                team.AvatarImagePath = null;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Avatar deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for team {TeamId}", id);
                return StatusCode(500, new { message = "Error deleting avatar" });
            }
        }

        // DELETE: api/team/{id}/background
        [HttpDelete("{id}/background")]
        public async Task<IActionResult> DeleteBackground(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Check if user is admin
            var userRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            if (userRole?.Role != TeamRole.Admin && team.CreatorId != userId)
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(team.BackgroundImagePath))
            {
                return BadRequest(new { message = "No background to delete" });
            }

            try
            {
                // Delete file
                var filePath = Path.Combine(_environment.WebRootPath, team.BackgroundImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Update database
                team.BackgroundImagePath = null;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Background deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting background for team {TeamId}", id);
                return StatusCode(500, new { message = "Error deleting background" });
            }
        }

        // DELETE: api/team/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Only creator can delete team
            if (team.CreatorId != userId)
            {
                return Forbid();
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Team deleted successfully" });
        }

        // GET: api/team/my-teams
        [HttpGet("my-teams")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyTeams()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var teams = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == userId)
                .Include(utr => utr.Team)
                .Select(utr => new
                {
                    utr.Team.Id,
                    utr.Team.Name,
                    utr.Team.Description,
                    utr.Team.AvatarImagePath,
                    utr.Team.BackgroundImagePath,
                    utr.Team.CreatedDate,
                    utr.Team.IsSystemTeam,
                    Role = utr.Role,
                    IsCreator = utr.Team.CreatorId == userId,
                    MemberCount = utr.Team.UserTeamRoles.Count
                })
                .ToListAsync();

            return Ok(teams);
        }

        // PUT: api/team/{id}/members/{userId}/role
        [HttpPut("{id}/members/{userId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int id, string userId, [FromBody] UpdateMemberRoleDto dto)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Check if current user is admin
            var currentUserRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == currentUserId);

            if (currentUserRole?.Role != TeamRole.Admin && team.CreatorId != currentUserId)
            {
                return Forbid();
            }

            // Cannot change creator's role
            if (team.CreatorId == userId)
            {
                return BadRequest(new { message = "Cannot change creator's role" });
            }

            var targetUserRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            if (targetUserRole == null)
            {
                return NotFound(new { message = "User is not a member of this team" });
            }

            targetUserRole.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Member role updated successfully" });
        }

        // POST: api/team/{id}/join
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinTeam(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Check if already a member
            var existingRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            if (existingRole != null)
            {
                return BadRequest(new { message = "Already a member of this team" });
            }

            // Add as member
            var userTeamRole = new UserTeamRole
            {
                AppUserId = userId,
                TeamId = id,
                Role = TeamRole.Member
            };

            _context.UserTeamRoles.Add(userTeamRole);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully joined team" });
        }

        // DELETE: api/team/{id}/leave
        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> LeaveTeam(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var team = await _context.Teams
                .Include(t => t.UserTeamRoles)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Cannot leave if you're the creator
            if (team.CreatorId == userId)
            {
                return BadRequest(new { message = "Team creator cannot leave the team" });
            }

            var userTeamRole = team.UserTeamRoles
                .FirstOrDefault(utr => utr.AppUserId == userId);

            if (userTeamRole == null)
            {
                return BadRequest(new { message = "You are not a member of this team" });
            }

            _context.UserTeamRoles.Remove(userTeamRole);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully left team" });
        }

        // GET: api/team/{id}/permissions
        [HttpGet("{id}/permissions")]
        public async Task<ActionResult<object>> GetTeamPermissions(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userTeamRole = await _context.UserTeamRoles
                .Include(utr => utr.UserTeamRolePermissions)
                    .ThenInclude(utrp => utrp.UserTeamPermission)
                .FirstOrDefaultAsync(utr => utr.TeamId == id && utr.AppUserId == userId);

            if (userTeamRole == null)
            {
                return Ok(new
                {
                    teamId = id,
                    isMember = false,
                    role = (TeamRole?)null,
                    permissions = new List<string>()
                });
            }

            var permissions = userTeamRole.UserTeamRolePermissions
                .Select(utrp => utrp.UserTeamPermission.PermissionName)
                .ToList();

            return Ok(new
            {
                teamId = id,
                isMember = true,
                role = userTeamRole.Role,
                permissions = permissions
            });
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TeamSearchDto>>> SearchTeams([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "Search query is required" });
                }

                var teams = await _context.Teams
                    .Where(t => t.Name.Contains(query) || t.Description.Contains(query))
                    .Select(t => new TeamSearchDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        MemberCount = t.Members.Count,
                        TitleCount = t.Titles.Count,
                        AvatarImagePath = t.AvatarImagePath
                    })
                    .OrderBy(t => t.Name)
                    .Take(50) // Limit results
                    .ToListAsync();

                return Ok(teams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching teams with query: {Query}", query);
                return StatusCode(500, new { message = "An error occurred while searching teams" });
            }
        }
    }



    // DTOs
    public class CreateTeamDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UpdateTeamDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UpdateMemberRoleDto
    {
        public TeamRole Role { get; set; }
    }
}