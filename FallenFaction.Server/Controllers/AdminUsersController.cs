using System.ComponentModel.DataAnnotations;
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
    public class AdminUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminUsersController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(
            ApplicationDbContext context,
            ILogger<AdminUsersController> logger,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Get all users with optional search
        /// GET: api/AdminUsers
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers(string? searchString = null)
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();

                if (!string.IsNullOrEmpty(searchString))
                {
                    var s = searchString.ToLower().TrimStart('@');
                    users = users.Where(u =>
                        (u.UserName != null && u.UserName.ToLower().Contains(s)) ||
                        (u.ProfileName != null && u.ProfileName.ToLower().Contains(s)) ||
                        (u.Email != null && u.Email.ToLower().Contains(s))).ToList();
                }

                var result = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    result.Add(new
                    {
                        id = user.Id,
                        userName = user.UserName,
                        profileName = user.ProfileName,
                        displayName = user.ProfileName ?? user.UserName,
                        email = user.Email,
                        roles = roles,
                        isBanned = !user.IsActive, // Assuming IsActive is the site ban
                        isBannedFromComments = user.IsBannedFromComments,
                        isOnline = user.IsOnline,
                        lastActive = user.LastActive,
                        registrationDate = user.RegistrationDate,
                        lastLoginDate = user.LastLoginDate,
                        isVerified = user.IsVerified,
                        profilePicturePath = user.ProfilePicturePath
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return StatusCode(500, new { message = "Error fetching users" });
            }
        }

        /// <summary>
        /// Search users by string
        /// GET: api/AdminUsers/SearchUser?searchString={searchString}
        /// </summary>
        [HttpGet("SearchUser")]
        public async Task<ActionResult<IEnumerable<object>>> SearchUser(string searchString)
        {
            return await GetUsers(searchString);
        }

        /// <summary>
        /// Get user by ID
        /// GET: api/AdminUsers/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUserById(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var result = new
                {
                    id = user.Id,
                    userName = user.UserName,
                    profileName = user.ProfileName,
                    displayName = user.ProfileName ?? user.UserName,
                    email = user.Email,
                    roles = roles,
                    isBanned = !user.IsActive,
                    isBannedFromComments = user.IsBannedFromComments,
                    isOnline = user.IsOnline,
                    lastActive = user.LastActive,
                    registrationDate = user.RegistrationDate,
                    lastLoginDate = user.LastLoginDate,
                    isVerified = user.IsVerified,
                    profilePicturePath = user.ProfilePicturePath,
                    bio = user.Bio,
                    dateOfBirth = user.DateOfBirth,
                    socialMediaLinks = user.SocialMediaLinks
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user details for ID: {Id}", id);
                return StatusCode(500, new { message = "Error fetching user details" });
            }
        }

        /// <summary>
        /// Ban user from site or comments
        /// POST: api/AdminUsers/BanUser
        /// </summary>
        [HttpPost("BanUser")]
        public async Task<ActionResult<object>> BanUser([FromBody] BanUserRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (request.BanType.ToLower() == "site")
                {
                    user.IsActive = false;
                }
                else if (request.BanType.ToLower() == "comments")
                {
                    user.IsBannedFromComments = true;
                }
                else
                {
                    return BadRequest(new { message = "Invalid ban type. Use 'site' or 'comments'" });
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} banned from {BanType} by admin", request.UserId, request.BanType);
                    return Ok(new { message = $"User banned from {request.BanType} successfully!" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to ban user", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning user {UserId} from {BanType}", request.UserId, request.BanType);
                return StatusCode(500, new { message = "Error banning user" });
            }
        }

        /// <summary>
        /// Unban user from site or comments
        /// POST: api/AdminUsers/UnbanUser
        /// </summary>
        [HttpPost("UnbanUser")]
        public async Task<ActionResult<object>> UnbanUser([FromBody] BanUserRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (request.BanType.ToLower() == "site")
                {
                    user.IsActive = true;
                }
                else if (request.BanType.ToLower() == "comments")
                {
                    user.IsBannedFromComments = false;
                }
                else
                {
                    return BadRequest(new { message = "Invalid ban type. Use 'site' or 'comments'" });
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} unbanned from {BanType} by admin", request.UserId, request.BanType);
                    return Ok(new { message = $"User unbanned from {request.BanType} successfully!" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to unban user", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning user {UserId} from {BanType}", request.UserId, request.BanType);
                return StatusCode(500, new { message = "Error unbanning user" });
            }
        }

        /// <summary>
        /// Change user role
        /// POST: api/AdminUsers/ChangeUserRole
        /// </summary>
        [HttpPost("ChangeUserRole")]
        public async Task<ActionResult<object>> ChangeUserRole([FromBody] ChangeRoleRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Check if role exists
                var roleExists = await _roleManager.RoleExistsAsync(request.Role);
                if (!roleExists)
                {
                    return BadRequest(new { message = $"Role '{request.Role}' does not exist" });
                }

                // Remove user from all existing roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        return BadRequest(new { message = "Failed to remove existing roles", errors = removeResult.Errors });
                    }
                }

                // Add user to new role (unless it's "User" which is default)
                if (request.Role != "User")
                {
                    var addResult = await _userManager.AddToRoleAsync(user, request.Role);
                    if (!addResult.Succeeded)
                    {
                        return BadRequest(new { message = "Failed to add new role", errors = addResult.Errors });
                    }
                }

                _logger.LogInformation("User {UserId} role changed to {Role} by admin", request.UserId, request.Role);
                return Ok(new { message = $"User role changed to {request.Role} successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user {UserId} to {Role}", request.UserId, request.Role);
                return StatusCode(500, new { message = "Error changing user role" });
            }
        }

        /// <summary>
        /// Delete user permanently
        /// DELETE: api/AdminUsers/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Prevent deleting the current admin user
                var currentUserId = _userManager.GetUserId(User);
                if (id == currentUserId)
                {
                    return BadRequest(new { message = "Cannot delete your own account" });
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} deleted by admin", id);
                    return Ok(new { message = "User deleted successfully!" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to delete user", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { message = "Error deleting user" });
            }
        }

        /// <summary>
        /// Get available roles
        /// GET: api/AdminUsers/Roles
        /// </summary>
        [HttpGet("Roles")]
        public async Task<ActionResult<IEnumerable<string>>> GetRoles()
        {
            try
            {
                var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching roles");
                return StatusCode(500, new { message = "Error fetching roles" });
            }
        }

        // Request DTOs
        public class BanUserRequest
        {
            [Required, StringLength(36)]
            public string UserId { get; set; } = string.Empty;

            [Required, StringLength(20)]
            public string BanType { get; set; } = string.Empty; // "site" or "comments"
        }

        public class ChangeRoleRequest
        {
            [Required, StringLength(36)]
            public string UserId { get; set; } = string.Empty;

            [Required, StringLength(20)]
            public string Role { get; set; } = string.Empty;
        }
    }
}