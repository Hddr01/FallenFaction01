// Fixed AuthController.cs - Addresses both POST/PATCH issue and background task disposal
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        // FIXED: Support both PATCH and POST methods for online status updates
        [HttpPatch("online-status")]
        [HttpPost("online-status")] // Add POST support for sendBeacon
        [AllowAnonymous]
        public async Task<ActionResult> UpdateOnlineStatus([FromBody] UpdateOnlineStatusRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogDebug("UpdateOnlineStatus: No user ID found in token");
                    return Ok(new { success = false, message = "No user session found" });
                }

                if (request == null)
                {
                    _logger.LogWarning("UpdateOnlineStatus: Null request body for user {UserId}, assuming offline", userId);
                    request = new UpdateOnlineStatusRequest { IsOnline = false };
                }

                _logger.LogDebug("Processing online status update for user {UserId}: {IsOnline}", userId, request.IsOnline);

                try
                {
                    var result = await _authService.UpdateOnlineStatusAsync(userId, request.IsOnline);
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

        // Test endpoint (remove in production)
        [HttpPost("test-status/{userId}")]
        [AllowAnonymous]
        public async Task<ActionResult> TestUpdateStatus(string userId, [FromBody] bool isOnline)
        {
            try
            {
                _logger.LogInformation("TEST: Updating status for user {UserId} to {IsOnline}", userId, isOnline);

                var result = await _authService.UpdateOnlineStatusAsync(userId, isOnline);

                if (result)
                {
                    var user = await _authService.GetUserProfileAsync(userId);

                    return Ok(new
                    {
                        success = true,
                        message = "Status updated successfully",
                        userId = userId,
                        newStatus = isOnline,
                        verifiedStatus = user?.IsOnline,
                        lastActive = user?.LastActive
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to update status" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TEST: Error updating status for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class UpdateOnlineStatusRequest
    {
        public bool IsOnline { get; set; } = false;
    }
}