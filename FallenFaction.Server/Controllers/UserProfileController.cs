using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Auth;
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(
            UserManager<AppUser> userManager,
            IWebHostEnvironment env,
            ILogger<UserProfileController> logger)
        {
            _userManager = userManager;
            _env = env;
            _logger = logger;
        }

        // ── GET current profile ───────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            return Ok(MapToDto(user));
        }

        // ── PUT update basic info ─────────────────────────────────────────────
        [HttpPut("UpdateProfile")]
        public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            user.ProfileName = req.ProfileName?.Trim();
            user.FirstName = req.FirstName?.Trim();
            user.LastName = req.LastName?.Trim();
            user.Bio = req.Bio?.Trim();

            if (req.DateOfBirth.HasValue)
                user.DateOfBirth = req.DateOfBirth;

            // Handle UserName change separately via Identity's SetUserNameAsync
            if (!string.IsNullOrWhiteSpace(req.UserName))
            {
                var newHandle = req.UserName.Trim();
                if (!string.Equals(user.UserName, newHandle, StringComparison.OrdinalIgnoreCase))
                {
                    var setResult = await _userManager.SetUserNameAsync(user, newHandle);
                    if (!setResult.Succeeded)
                        return BadRequest(new { message = string.Join(", ", setResult.Errors.Select(e => e.Description)) });
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Ok(MapToDto(user));
        }

        // ── POST change password ──────────────────────────────────────────────
        [HttpPost("ChangePassword")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Ok(new { message = "Password changed successfully." });
        }

        // ── POST upload avatar ────────────────────────────────────────────────
        [HttpPost("UploadAvatar")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File too large. Maximum size is 5 MB." });

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Invalid file type. Allowed: jpg, png, webp, gif." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                var path = await SaveImageAsync(file, "avatars");
                user.ProfilePicturePath = path;
                await _userManager.UpdateAsync(user);
                return Ok(new { profilePicturePath = path, message = "Avatar updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user {UserId}", user.Id);
                return StatusCode(500, new { message = "Failed to upload avatar." });
            }
        }

        // ── POST upload profile banner ────────────────────────────────────────
        [HttpPost("UploadBanner")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadBanner(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "File too large. Maximum size is 10 MB." });

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Invalid file type. Allowed: jpg, png, webp." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                var path = await SaveImageAsync(file, "banners");
                user.BannerImagePath = path;
                await _userManager.UpdateAsync(user);
                return Ok(new { bannerImagePath = path, message = "Banner updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading banner for user {UserId}", user.Id);
                return StatusCode(500, new { message = "Failed to upload banner." });
            }
        }

        // ── helpers ───────────────────────────────────────────────────────────
        private async Task<string> SaveImageAsync(IFormFile image, string folder)
        {
            var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
            var filePath = Path.Combine(dir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }

        private static UserProfileDto MapToDto(AppUser u) => new()
        {
            Id = u.Id,
            UserName = u.UserName ?? "",
            ProfileName = u.ProfileName,
            Email = u.Email ?? "",
            FirstName = u.FirstName,
            LastName = u.LastName,
            Bio = u.Bio,
            DateOfBirth = u.DateOfBirth,
            ProfilePicturePath = u.ProfilePicturePath,
            BannerImagePath = u.BannerImagePath,
            RegistrationDate = u.RegistrationDate,
            LastLoginDate = u.LastLoginDate,
            IsOnline = u.IsOnline,
            IsActive = u.IsActive,
            IsVerified = u.IsVerified,
        };
    }

    // ── Request / Response DTOs ───────────────────────────────────────────────

    public class UpdateProfileRequest
    {
        [StringLength(50)] public string? ProfileName { get; set; }
        [StringLength(30, MinimumLength = 3), RegularExpression(@"^[a-zA-Z0-9_\-]+$",
            ErrorMessage = "Username may only contain letters, numbers, underscores and hyphens.")]
        public string? UserName { get; set; }
        [StringLength(50)] public string? FirstName { get; set; }
        [StringLength(50)] public string? LastName { get; set; }
        [StringLength(500)] public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required] public string CurrentPassword { get; set; } = "";
        [Required, MinLength(6)] public string NewPassword { get; set; } = "";
        [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
    }

    public class UserProfileDto
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? ProfileName { get; set; }
        public string Email { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePicturePath { get; set; }
        public string? BannerImagePath { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public bool IsOnline { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
    }
}