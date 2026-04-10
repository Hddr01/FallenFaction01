// Fixed AuthController.cs - Addresses both POST/PATCH issue and background task disposal
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using FallenFaction.Server.DTOs.Auth;
using FallenFaction.Server.Services.Interfaces;
using System.Security.Claims;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory; // Use IServiceScopeFactory instead

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger,
            IServiceScopeFactory serviceScopeFactory) // Change to IServiceScopeFactory
        {
            _authService = authService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpPost("register")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid input data",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            try
            {
                var result = await _authService.RegisterAsync(registerDto);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during registration",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPost("accept-terms")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<AuthResponseDto>> AcceptTerms([FromBody] AcceptTermsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid input data",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            try
            {
                var result = await _authService.AcceptTermsAndLoginAsync(dto);

                if (result.Success)
                {
                    return Ok(result);
                }

                return Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during accept terms");
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid input data",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            try
            {
                var result = await _authService.LoginAsync(loginDto);

                if (result.Success)
                {
                    return Ok(result);
                }

                return Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                _logger.LogInformation("Logout request received for user {UserId} ({Email})", userId, userEmail);

                // FIXED: Process logout synchronously to ensure database update
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        var logoutResult = await _authService.LogoutAsync(userId);
                        if (logoutResult)
                        {
                            _logger.LogInformation("User {UserId} logged out successfully with database update", userId);
                        }
                        else
                        {
                            _logger.LogWarning("Logout completed for user {UserId} but database update may have failed", userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during logout process for user {UserId}", userId);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Logout successful",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during logout");
                return Ok(new
                {
                    success = true,
                    message = "Logout completed",
                    note = "Session cleared despite server error"
                });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Invalid user");
                }

                var user = await _authService.GetUserProfileAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new { message = "An error occurred while retrieving profile" });
            }
        }

        [HttpGet("validate-token")]
        [Authorize]
        public ActionResult ValidateToken()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;

                return Ok(new
                {
                    valid = true,
                    userId = userId,
                    email = email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return Unauthorized(new { valid = false });
            }
        }

        [HttpPatch("online-status")]
        [HttpPost("online-status")]
        [AllowAnonymous]
        public async Task<ActionResult> UpdateOnlineStatus(
            [FromQuery] bool isOnline = true)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogDebug("UpdateOnlineStatus: No user ID found in token");
                    return Ok(new { success = false, message = "No user session found" });
                }

                var statusIsOnline = isOnline;

                _logger.LogDebug("Processing online status update for user {UserId}: {IsOnline}", userId, statusIsOnline);

                try
                {
                    var result = await _authService.UpdateOnlineStatusAsync(userId, statusIsOnline);
                    if (!result)
                    {
                        _logger.LogWarning("Failed to update online status for user {UserId}", userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating online status for user {UserId}", userId);
                }

                return Ok(new
                {
                    success = true,
                    message = "Status update completed",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateOnlineStatus for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                return Ok(new
                {
                    success = true,
                    message = "Status update attempted",
                    note = "Server error occurred but request acknowledged"
                });
            }
        }

        // Add heartbeat endpoint
        [HttpPost("heartbeat")]
        [Authorize]
        public async Task<ActionResult> Heartbeat()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new { success = false, message = "No user session found" });
                }

                var result = await _authService.UpdateLastActiveAsync(userId);

                return Ok(new
                {
                    success = result,
                    message = "Heartbeat processed",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return Ok(new { success = false, message = "Heartbeat failed" });
            }
        }

        // ── Email confirmation ────────────────────────────────────────────────

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest(new AuthResponseDto { Success = false, Message = "Invalid confirmation link." });

            var result = await _authService.ConfirmEmailAsync(userId, token);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<AuthResponseDto>> ResendConfirmation([FromBody] ResendConfirmationDto dto)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(dto?.Email))
                return BadRequest(new AuthResponseDto { Success = false, Message = "Email is required." });

            var result = await _authService.ResendConfirmationEmailAsync(dto.Email);
            return Ok(result); // always 200 to prevent email enumeration
        }

        // Add health check endpoint
        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "AuthController"
            });
        }

    }

    public record ResendConfirmationDto([property: System.ComponentModel.DataAnnotations.EmailAddress][property: System.ComponentModel.DataAnnotations.StringLength(254)] string? Email);
}