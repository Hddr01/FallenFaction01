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
        private readonly Random _random;

        public UsersController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _random = new Random();
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
                    .OrderBy(u => _random.Next()) // Random order
                    .Take(12)
                    .Select(u => new UserTopDto
                    {
                        Id = u.Id,
                        Name = u.UserName ?? "Unknown User",
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
                return StatusCode(500, new { message = "Error fetching top users", error = ex.Message });
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
                    Name = user.UserName ?? "Unknown User",
                    Avatar = user.ProfilePicturePath ?? "/img/logo.png",
                    Level = GetUserLevel(user.Id),
                    Score = GetUserScore(user.Id)
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}: {Error}", id, ex.Message);
                return StatusCode(500, new { message = "Error fetching user", error = ex.Message });
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

                var q = query.Trim().ToLower();

                var users = await _userManager.Users
                    .Where(u => !string.IsNullOrEmpty(u.UserName) &&
                                u.UserName.ToLower().Contains(q))
                    .Take(10)
                    .ToListAsync();

                var results = users.Select(u => new UserTopDto
                {
                    Id = u.Id,
                    Name = u.UserName ?? "Unknown User",
                    Avatar = u.ProfilePicturePath ?? "/img/logo.png",
                    Level = GetUserLevel(u.Id),
                    Score = GetUserScore(u.Id),
                }).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with query: {Query}", query);
                return StatusCode(500, new { message = "Error searching users", error = ex.Message });
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

        /// <summary>
        /// Create sample users for testing
        /// POST: api/Users/CreateSampleUsers
        /// </summary>
        [HttpPost("CreateSampleUsers")]
        public async Task<ActionResult> CreateSampleUsers()
        {
            try
            {
                var userCount = await _userManager.Users.CountAsync();
                if (userCount > 5)
                {
                    return Ok(new { message = $"Already have {userCount} users. No need to create more." });
                }

                var sampleUsers = new List<(string username, string email)>
                {
                    ("MangaFan123", "mangafan123@example.com"),
                    ("OtakuMaster", "otakumaster@example.com"),
                    ("AnimeGirl", "animegirl@example.com"),
                    ("SenpaiKun", "senpaikon@example.com"),
                    ("WeebLord", "weeblord@example.com"),
                    ("NarutoFan", "narutofan@example.com"),
                    ("OnePieceKing", "onepieceking@example.com"),
                    ("AttackOnFan", "attackonfan@example.com"),
                    ("DemonSlayer", "demonslayer@example.com"),
                    ("HeroAcademy", "heroacademy@example.com"),
                    ("DragonBallZ", "dragonballz@example.com"),
                    ("TokyoGhoul", "tokyoghoul@example.com")
                };

                var createdUsers = new List<string>();

                foreach (var (username, email) in sampleUsers)
                {
                    var existingUser = await _userManager.FindByNameAsync(username);
                    if (existingUser == null)
                    {
                        var user = new AppUser
                        {
                            UserName = username,
                            Email = email,
                            EmailConfirmed = true,
                            ProfilePicturePath = "/img/logo.png"
                        };

                        // Set additional properties if they exist
                        var isActiveProperty = typeof(AppUser).GetProperty("IsActive");
                        if (isActiveProperty != null)
                        {
                            isActiveProperty.SetValue(user, true);
                        }

                        var isVerifiedProperty = typeof(AppUser).GetProperty("IsVerified");
                        if (isVerifiedProperty != null)
                        {
                            isVerifiedProperty.SetValue(user, true);
                        }

                        var registrationDateProperty = typeof(AppUser).GetProperty("RegistrationDate");
                        if (registrationDateProperty != null)
                        {
                            registrationDateProperty.SetValue(user, DateTime.UtcNow.AddDays(-_random.Next(1, 365)));
                        }

                        var result = await _userManager.CreateAsync(user, "TempPassword123!");
                        if (result.Succeeded)
                        {
                            createdUsers.Add(username);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create user {Username}: {Errors}", username, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                return Ok(new
                {
                    message = $"Created {createdUsers.Count} sample users",
                    users = createdUsers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample users: {Error}", ex.Message);
                return StatusCode(500, new { error = "Error creating sample users", details = ex.Message });
            }
        }

        /// <summary>
        /// Get debug info about users
        /// GET: api/Users/Debug
        /// </summary>
        [HttpGet("Debug")]
        public async Task<ActionResult> GetDebugInfo()
        {
            try
            {
                var totalUsers = await _userManager.Users.CountAsync();

                var sampleUsers = await _userManager.Users
                    .Take(5)
                    .Select(u => new { u.Id, u.UserName, u.Email })
                    .ToListAsync();

                return Ok(new
                {
                    TotalUsers = totalUsers,
                    SampleUsers = sampleUsers,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user debug info: {Error}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
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