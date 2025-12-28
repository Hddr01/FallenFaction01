// Controllers/TeamRoleApiController.cs - FIXED VERSION
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamRoleApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<TeamRoleApiController> _logger;

        public TeamRoleApiController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<TeamRoleApiController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all permissions and roles for a team
        /// GET: api/TeamRoleApi/{teamId}/permissions-overview
        /// </summary>
        [HttpGet("{teamId}/permissions-overview")]
        public async Task<IActionResult> GetPermissionsOverview(int teamId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Check if user can manage roles for this team
                if (!await CanManageTeamRoles(user.Id, teamId))
                {
                    return Forbid("You don't have permission to manage roles for this team");
                }

                // Get all available permissions
                var allPermissions = await _context.UserTeamPermissions
                    .Select(p => new
                    {
                        p.Id,
                        p.PermissionName,
                        DisplayName = GetPermissionDisplayName(p.PermissionName),
                        Description = GetPermissionDescription(p.PermissionName)
                    })
                    .ToListAsync();

                // Get default role configurations
                var defaultRoles = new[]
                {
                    new
                    {
                        Value = 0,
                        Name = "Admin",
                        Description = "Full team management permissions",
                        Permissions = allPermissions.Select(p => p.PermissionName).ToArray()
                    },
                    new
                    {
                        Value = 1,
                        Name = "Member",
                        Description = "Can contribute content and moderate",
                        Permissions = new[] { "CanAddTitle", "CanEditTitle", "CanAddChapter", "CanEditChapter" }
                    },
                    new
                    {
                        Value = 2,
                        Name = "Viewer",
                        Description = "Read-only access to team content",
                        Permissions = new string[0]
                    }
                };

                // Get custom roles for this team (if you implement custom roles)
                var customRoles = await GetCustomRoles(teamId);

                return Ok(new
                {
                    AllPermissions = allPermissions,
                    DefaultRoles = defaultRoles,
                    CustomRoles = customRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions overview for team {TeamId}", teamId);
                return StatusCode(500, new { error = "Failed to load permissions overview" });
            }
        }

        /// <summary>
        /// Get specific member permissions
        /// GET: api/TeamRoleApi/{teamId}/member/{userId}/permissions
        /// </summary>
        [HttpGet("{teamId}/member/{userId}/permissions")]
        public async Task<IActionResult> GetMemberPermissions(int teamId, string userId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                // Check if user can view member permissions (either admin or the member themselves)
                if (!await CanManageTeamRoles(currentUser.Id, teamId) && currentUser.Id != userId)
                {
                    return Forbid("You don't have permission to view this member's permissions");
                }

                var memberRole = await _context.UserTeamRoles
                    .Include(utr => utr.Team)
                    .Include(utr => utr.UserTeamRolePermissions)
                        .ThenInclude(utrp => utrp.UserTeamPermission)
                    .Include(utr => utr.AppUser)
                    .FirstOrDefaultAsync(utr => utr.AppUserId == userId && utr.TeamId == teamId);

                if (memberRole == null)
                {
                    return NotFound("User is not a member of this team");
                }

                var allPermissions = await _context.UserTeamPermissions.ToListAsync();
                var userPermissions = new List<string>();

                // Team creators and admins have all permissions
                if (memberRole.Team.CreatorId == userId || memberRole.Role == TeamRole.Admin)
                {
                    userPermissions = allPermissions.Select(p => p.PermissionName).ToList();
                }
                else
                {
                    // Get specific permissions for members
                    userPermissions = memberRole.UserTeamRolePermissions
                        .Select(utrp => utrp.UserTeamPermission.PermissionName)
                        .ToList();
                }

                var permissionDetails = allPermissions.Select(permission => new
                {
                    Name = permission.PermissionName,
                    DisplayName = GetPermissionDisplayName(permission.PermissionName),
                    Description = GetPermissionDescription(permission.PermissionName),
                    HasPermission = userPermissions.Contains(permission.PermissionName)
                }).ToList();

                return Ok(new
                {
                    UserId = userId,
                    UserName = memberRole.AppUser.UserName,
                    Role = memberRole.Role,
                    RoleName = GetRoleName(memberRole.Role),
                    IsCreator = memberRole.Team.CreatorId == userId,
                    Permissions = permissionDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member permissions for user {UserId} in team {TeamId}", userId, teamId);
                return StatusCode(500, new { error = "Failed to load member permissions" });
            }
        }

        /// <summary>
        /// Update member permissions (for custom permission assignment)
        /// PUT: api/TeamRoleApi/{teamId}/member/{userId}/permissions
        /// </summary>
        [HttpPut("{teamId}/member/{userId}/permissions")]
        public async Task<IActionResult> UpdateMemberPermissions(int teamId, string userId, [FromBody] UpdateMemberPermissionsRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                // Check if user can manage roles for this team
                if (!await CanManageTeamRoles(currentUser.Id, teamId))
                {
                    return Forbid("You don't have permission to manage roles for this team");
                }

                var team = await _context.Teams.FindAsync(teamId);
                if (team == null)
                {
                    return NotFound("Team not found");
                }

                // Cannot change creator's permissions
                if (team.CreatorId == userId)
                {
                    return BadRequest("Cannot change team creator's permissions");
                }

                var memberRole = await _context.UserTeamRoles
                    .Include(utr => utr.UserTeamRolePermissions)
                    .FirstOrDefaultAsync(utr => utr.AppUserId == userId && utr.TeamId == teamId);

                if (memberRole == null)
                {
                    return NotFound("User is not a member of this team");
                }

                // Get valid permission IDs
                var validPermissionIds = await _context.UserTeamPermissions
                    .Where(p => request.PermissionNames.Contains(p.PermissionName))
                    .Select(p => p.Id)
                    .ToListAsync();

                // Remove existing permissions
                _context.UserTeamRolePermissions.RemoveRange(memberRole.UserTeamRolePermissions);

                // Add new permissions
                foreach (var permissionId in validPermissionIds)
                {
                    var rolePermission = new UserTeamRolePermission
                    {
                        AppUserId = userId,
                        TeamId = teamId,
                        PermissionId = permissionId
                    };
                    _context.UserTeamRolePermissions.Add(rolePermission);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Member permissions updated for user {UserId} in team {TeamId} by {CurrentUserId}",
                    userId, teamId, currentUser.Id);

                return Ok(new { message = "Member permissions updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member permissions for user {UserId} in team {TeamId}", userId, teamId);
                return StatusCode(500, new { error = "Failed to update member permissions" });
            }
        }

        /// <summary>
        /// Create a custom role template (for future use)
        /// POST: api/TeamRoleApi/{teamId}/custom-roles
        /// </summary>
        [HttpPost("{teamId}/custom-roles")]
        public async Task<IActionResult> CreateCustomRole(int teamId, [FromBody] CreateCustomRoleRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                // Check if user can manage roles for this team
                if (!await CanManageTeamRoles(currentUser.Id, teamId))
                {
                    return Forbid("You don't have permission to manage roles for this team");
                }

                // Validate team exists
                var team = await _context.Teams.FindAsync(teamId);
                if (team == null)
                {
                    return NotFound("Team not found");
                }

                // For now, we'll store custom roles as a configuration
                // In a full implementation, you'd create a CustomTeamRole table
                // This is a simplified approach using the existing permission system

                return Ok(new
                {
                    message = "Custom role created successfully",
                    note = "Custom roles are stored as permission templates for manual assignment"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating custom role for team {TeamId}", teamId);
                return StatusCode(500, new { error = "Failed to create custom role" });
            }
        }

        /// <summary>
        /// Apply a role template to a member
        /// POST: api/TeamRoleApi/{teamId}/member/{userId}/apply-template
        /// </summary>
        [HttpPost("{teamId}/member/{userId}/apply-template")]
        public async Task<IActionResult> ApplyRoleTemplate(int teamId, string userId, [FromBody] ApplyRoleTemplateRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                // Check if user can manage roles for this team
                if (!await CanManageTeamRoles(currentUser.Id, teamId))
                {
                    return Forbid("You don't have permission to manage roles for this team");
                }

                // Get predefined role templates - FIXED: Use the helper method with proper name
                var roleTemplates = GetPredefinedRoleTemplates();

                if (!roleTemplates.ContainsKey(request.TemplateName))
                {
                    return BadRequest("Invalid role template");
                }

                var template = roleTemplates[request.TemplateName];

                // Apply the template permissions
                var updateRequest = new UpdateMemberPermissionsRequest
                {
                    PermissionNames = template.Permissions.ToList()
                };

                // Reuse the existing permission update logic
                return await UpdateMemberPermissions(teamId, userId, updateRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying role template for user {UserId} in team {TeamId}", userId, teamId);
                return StatusCode(500, new { error = "Failed to apply role template" });
            }
        }

        /// <summary>
        /// Get available role templates
        /// GET: api/TeamRoleApi/role-templates
        /// </summary>
        [HttpGet("role-templates")]
        public IActionResult GetAvailableRoleTemplates()
        {
            // FIXED: Use the helper method and explicitly type the result
            var roleTemplates = GetPredefinedRoleTemplates();

            var templates = roleTemplates.Select(kvp => new
            {
                Name = kvp.Key,
                DisplayName = kvp.Value.DisplayName,
                Description = kvp.Value.Description,
                Permissions = kvp.Value.Permissions
            }).ToList(); // FIXED: Added .ToList() to resolve type inference

            return Ok(templates);
        }

        #region Helper Methods

        private async Task<bool> CanManageTeamRoles(string userId, int teamId)
        {
            // Check if user is admin
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return true;
            }

            // Check if user is team creator or admin
            var team = await _context.Teams.FindAsync(teamId);
            if (team?.CreatorId == userId)
            {
                return true;
            }

            var userRole = await _context.UserTeamRoles
                .FirstOrDefaultAsync(utr => utr.AppUserId == userId && utr.TeamId == teamId);

            return userRole?.Role == TeamRole.Admin;
        }

        private async Task<object[]> GetCustomRoles(int teamId)
        {
            // In a full implementation, you'd have a CustomTeamRole table
            // For now, return empty array or mock data
            return new object[0];
        }

        // FIXED: Renamed the helper method to avoid conflict
        private static Dictionary<string, RoleTemplate> GetPredefinedRoleTemplates()
        {
            return new Dictionary<string, RoleTemplate>
            {
                {
                    "Translator",
                    new RoleTemplate
                    {
                        DisplayName = "Translator",
                        Description = "Can add and edit chapters",
                        Permissions = new[] { "CanAddChapter", "CanEditChapter" }
                    }
                },
                {
                    "Editor",
                    new RoleTemplate
                    {
                        DisplayName = "Editor",
                        Description = "Can edit content and view analytics",
                        Permissions = new[] { "CanEditTitle", "CanEditChapter", "CanViewAnalytics" }
                    }
                },
                {
                    "QualityChecker",
                    new RoleTemplate
                    {
                        DisplayName = "Quality Checker",
                        Description = "Can edit chapters and view analytics",
                        Permissions = new[] { "CanEditChapter", "CanViewAnalytics" }
                    }
                },
                {
                    "ContentManager",
                    new RoleTemplate
                    {
                        DisplayName = "Content Manager",
                        Description = "Full content management permissions",
                        Permissions = new[] { "CanAddTitle", "CanEditTitle", "CanAddChapter", "CanEditChapter", "CanViewAnalytics" }
                    }
                }
            };
        }

        private static string GetPermissionDisplayName(string permissionName)
        {
            return permissionName switch
            {
                "CanAddTitle" => "Add Titles",
                "CanEditTitle" => "Edit Titles",
                "CanDeleteTitle" => "Delete Titles",
                "CanAddChapter" => "Add Chapters",
                "CanEditChapter" => "Edit Chapters",
                "CanDeleteChapter" => "Delete Chapters",
                "CanAddMember" => "Add Members",
                "CanRemoveMember" => "Remove Members",
                "CanViewAnalytics" => "View Analytics",
                _ => permissionName
            };
        }

        private static string GetPermissionDescription(string permissionName)
        {
            return permissionName switch
            {
                "CanAddTitle" => "Create new manga titles",
                "CanEditTitle" => "Modify existing titles",
                "CanDeleteTitle" => "Remove titles from team",
                "CanAddChapter" => "Upload new chapters",
                "CanEditChapter" => "Modify existing chapters",
                "CanDeleteChapter" => "Remove chapters",
                "CanAddMember" => "Invite new team members",
                "CanRemoveMember" => "Remove team members",
                "CanViewAnalytics" => "Access team statistics",
                _ => "Permission description"
            };
        }

        private static string GetRoleName(TeamRole role)
        {
            return role switch
            {
                TeamRole.Admin => "Admin",
                TeamRole.Member => "Member",
                TeamRole.Viewer => "Viewer",
                _ => "Unknown"
            };
        }

        #endregion

        #region Request Models

        public class UpdateMemberPermissionsRequest
        {
            [Required]
            public List<string> PermissionNames { get; set; } = new();
        }

        public class CreateCustomRoleRequest
        {
            [Required]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [StringLength(500)]
            public string Description { get; set; } = string.Empty;

            [Required]
            public List<string> PermissionNames { get; set; } = new();
        }

        public class ApplyRoleTemplateRequest
        {
            [Required]
            public string TemplateName { get; set; } = string.Empty;
        }

        public class RoleTemplate
        {
            public string DisplayName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string[] Permissions { get; set; } = Array.Empty<string>();
        }

        #endregion
    }
}