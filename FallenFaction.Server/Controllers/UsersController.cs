// Controllers/UsersController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.User;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get top users for homepage
        /// GET: api/Users/TopUsers
        /// </summary>
        [HttpGet("TopUsers")]
        public async Task<ActionResult<IEnumerable<UserTopDto>>> GetTopUsers()
        {
            try
            {
                _logger.LogInformation("Fetching top users");

                var userCount = await _userManager.Users.CountAsync();
                _logger.LogInformation($"Total users in database: {userCount}");

                if (userCount == 0)
                {
                    _logger.LogWarning("No users found in database");
                    return Ok(new List<UserTopDto>());
                }

                var users = await _userManager.Users
                    .Where(u => !string.IsNullOrEmpty(u.UserName))
                    .ToListAsync();

                // Filter active users if the properties exist, otherwise use all users
                var activeUsers = users.Where(u =>
                {
                    // Check if IsActive property exists and use it, otherwise assume active
                    var isActiveProperty = typeof(AppUser).GetProperty("IsActive");
                    if (isActiveProperty != null)
                    {
                        return (bool)(isActiveProperty.GetValue(u) ?? true);
                    }
                    return true;
                }).ToList();

                var topUsers = activeUsers
                    .OrderBy(u => Random.Shared.Next()) // Random order
                    .Take(12)
                    .Select(u => new UserTopDto
                    {
                        Id = u.Id,
                        Name = u.ProfileName ?? u.UserName ?? "Unknown User",
                        UserName = u.UserName ?? "",
                        Avatar = u.ProfilePicturePath ?? "/img/logo.png",
                        Level = GetUserLevel(u.Id),
                        Score = GetUserScore(u.Id)
                    })
                    .ToList();

                _logger.LogInformation($"Returning {topUsers.Count} top users");
                return Ok(topUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top users: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching top users" });
            }
        }

        /// <summary>
        /// Get user profile information (public endpoint)
        /// GET: api/Users/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserTopDto>> GetUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Check if user is active (if property exists)
                var isActiveProperty = typeof(AppUser).GetProperty("IsActive");
                bool isActive = true;
                if (isActiveProperty != null)
                {
                    isActive = (bool)(isActiveProperty.GetValue(user) ?? true);
                }

                if (!isActive)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userDto = new UserTopDto
                {
                    Id = user.Id,
                    Name = user.ProfileName ?? user.UserName ?? "Unknown User",
                    UserName = user.UserName ?? "",
                    Avatar = user.ProfilePicturePath ?? "/img/logo.png",
                    Level = GetUserLevel(user.Id),
                    Score = GetUserScore(user.Id)
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}: {Error}", id, ex.Message);
                return StatusCode(500, new { message = "Error fetching user" });
            }
        }

        /// <summary>
        /// Get full public profile for a user.
        /// GET: api/Users/{id}/profile
        /// </summary>
        [HttpGet("{id}/profile")]
        public async Task<ActionResult<PublicUserProfileDto>> GetPublicProfile(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null || !user.IsActive)
                    return NotFound(new { message = "User not found" });

                var dto = new PublicUserProfileDto
                {
                    Id = user.Id,
                    Name = user.ProfileName ?? user.UserName ?? "Unknown",
                    UserName = user.UserName ?? "",
                    Avatar = user.ProfilePicturePath ?? "/img/default-avatar.png",
                    Banner = user.BannerImagePath,
                    Bio = user.Bio,
                    Level = user.UserLevel,
                    XpPoints = user.XpPoints,
                    IsOnline = user.IsOnline,
                    RegistrationDate = user.RegistrationDate,
                    IsVerified = user.IsVerified
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching public profile for {UserId}", id);
                return StatusCode(500, new { message = "Error fetching user profile" });
            }
        }

        /// <summary>
        /// Search users by username for global search bar.
        /// GET: api/Users/search?query=...
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserTopDto>>> SearchUsers([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                    return Ok(new List<UserTopDto>());
                if (query.Length > 100)
                    return BadRequest(new { message = "Search query must not exceed 100 characters." });

                var q = query.Trim().ToLower();

                // Strip leading @ so searching "@admin" matches "admin"
                var qClean = q.TrimStart('@');

                var users = await _userManager.Users
                    .Where(u => !string.IsNullOrEmpty(u.UserName) &&
                                (u.UserName.ToLower().Contains(qClean) ||
                                 (u.ProfileName != null && u.ProfileName.ToLower().Contains(qClean))))
                    .Take(10)
                    .ToListAsync();

                var results = users.Select(u => new UserTopDto
                {
                    Id = u.Id,
                    Name = u.ProfileName ?? u.UserName ?? "Unknown User",
                    UserName = u.UserName ?? "",
                    Avatar = u.ProfilePicturePath ?? "/img/logo.png",
                    Level = GetUserLevel(u.Id),
                    Score = GetUserScore(u.Id),
                }).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with query: {Query}", query);
                return StatusCode(500, new { message = "Error searching users" });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// GET: api/Users/health
        /// </summary>
        [HttpGet("health")]
        public ActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "UsersController"
            });
        }

        // Helper methods for mock data - replace with actual logic
        private int GetUserLevel(string userId)
        {
            // Mock level calculation based on user ID hash for consistency
            var hash = userId.GetHashCode();
            return Math.Abs(hash % 10) + 1; // Level 1-10
        }

        private string GetUserScore(string userId)
        {
            // Mock score based on user ID hash for consistency
            var hash = userId.GetHashCode();
            var current = Math.Abs(hash % 60) + 80; // 80-139
            var max = Math.Abs(hash % 50) + 150; // 150-199
            return $"{current}/{max}";
        }
    }
}