using Microsoft.AspNetCore.Mvc;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FallenFaction.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminTitleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<AdminTitleController> _logger;
        private readonly UserManager<AppUser> _userManager;

        public AdminTitleController(
            ApplicationDbContext context,
            IWebHostEnvironment hostingEnvironment,
            ILogger<AdminTitleController> logger,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Get all approved titles for admin management
        /// GET: api/AdminTitle/AdminTitleManagement
        /// </summary>
        [HttpGet("AdminTitleManagement")]
        public async Task<ActionResult<IEnumerable<object>>> GetApprovedTitles()
        {
            try
            {
                var titles = await _context.Titles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .Select(t => new
                    {
                        id = t.Id,
                        originalTitle = t.OriginalTitle,
                        englishTitle = t.EnglishTitle,
                        alternativeNames = t.AlternativeNames,
                        releaseDate = t.ReleaseDate,
                        description = t.Description,
                        statusTitle = t.StatusTitle,
                        statusTranslation = t.StatusTranslation,
                        type = t.Type,
                        ageRestriction = t.AgeRestriction,
                        externalLinks = t.ExternalLinksSerialized,
                        coverImagePath = t.CoverImagePath,
                        backgroundImagePath = t.BackgroundImagePath,
                        isAvailable = t.IsAvailable,
                        areCommentsEnabled = t.AreCommentsEnabled,
                        areChapterCommentsEnabled = t.AreChapterCommentsEnabled,
                        categories = t.Categories.Select(c => new { id = c.Id, name = c.Name }),
                        tags = t.Tags.Select(tag => new { id = tag.Id, name = tag.Name }),
                        formats = t.Formats.Select(f => new { id = f.Id, name = f.Name }),
                        authors = t.Authors.Select(a => new { id = a.Id, name = a.Name }),
                        artists = t.Artists.Select(a => new { id = a.Id, name = a.Name }),
                        publishers = t.Publishers.Select(p => new { id = p.Id, name = p.Name }),
                        teams = t.Teams.Select(team => new { id = team.Id, name = team.Name })
                    })
                    .ToListAsync();

                return Ok(titles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching approved titles");
                return StatusCode(500, new { message = "Error fetching approved titles", error = ex.Message });
            }
        }

        /// <summary>
        /// Get pending title details by ID
        /// GET: api/AdminTitle/GetPendingTitleDetails?id={id}
        /// </summary>
        [HttpGet("GetPendingTitleDetails")]
        public async Task<ActionResult<object>> GetPendingTitleDetails(int id)
        {
            try
            {
                var pendingTitle = await _context.PendingTitles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (pendingTitle == null)
                {
                    return NotFound(new { message = "Pending title not found" });
                }

                var result = new
                {
                    id = pendingTitle.Id,
                    originalTitle = pendingTitle.OriginalTitle,
                    englishTitle = pendingTitle.EnglishTitle,
                    alternativeNames = pendingTitle.AlternativeNames,
                    releaseDate = pendingTitle.ReleaseDate,
                    description = pendingTitle.Description,
                    statusTitle = pendingTitle.StatusTitle,
                    statusTranslation = pendingTitle.StatusTranslation,
                    type = pendingTitle.Type,
                    ageRestriction = pendingTitle.AgeRestriction,
                    externalLinks = pendingTitle.ExternalLinksSerialized,
                    coverImagePath = pendingTitle.CoverImagePath,
                    backgroundImagePath = pendingTitle.BackgroundImagePath,
                    categories = pendingTitle.Categories?.Select(c => new { id = c.Id, name = c.Name }).Cast<object>().ToList() ?? new List<object>(),
                    tags = pendingTitle.Tags?.Select(t => new { id = t.Id, name = t.Name }).Cast<object>().ToList() ?? new List<object>(),
                    formats = pendingTitle.Formats?.Select(f => new { id = f.Id, name = f.Name }).Cast<object>().ToList() ?? new List<object>(),
                    authors = pendingTitle.Authors?.Select(a => new { id = a.Id, name = a.Name }).Cast<object>().ToList() ?? new List<object>(),
                    artists = pendingTitle.Artists?.Select(a => new { id = a.Id, name = a.Name }).Cast<object>().ToList() ?? new List<object>(),
                    publishers = pendingTitle.Publishers?.Select(p => new { id = p.Id, name = p.Name }).Cast<object>().ToList() ?? new List<object>(),
                    teams = pendingTitle.Teams?.Select(t => new { id = t.Id, name = t.Name }).Cast<object>().ToList() ?? new List<object>()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending title details for ID: {Id}", id);
                return StatusCode(500, new { message = "Error fetching pending title details", error = ex.Message });
            }
        }

        /// <summary>
        /// Reject a pending title
        /// POST: api/AdminTitle/RejectTitle
        /// </summary>
        [HttpPost("RejectTitle")]
        public async Task<ActionResult<object>> RejectTitle([FromBody] AdminRejectTitleRequest request)
        {
            try
            {
                var pendingTitle = await _context.PendingTitles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .FirstOrDefaultAsync(t => t.Id == request.Id);

                if (pendingTitle == null)
                {
                    return NotFound(new { message = "Pending title not found" });
                }

                var rejectedTitle = new RejectedTitle
                {
                    OriginalTitle = pendingTitle.OriginalTitle,
                    EnglishTitle = pendingTitle.EnglishTitle,
                    AlternativeNames = pendingTitle.AlternativeNames,
                    ReleaseDate = pendingTitle.ReleaseDate,
                    Description = pendingTitle.Description,
                    StatusTitle = "Rejected",
                    StatusTranslation = pendingTitle.StatusTranslation,
                    Type = pendingTitle.Type,
                    AgeRestriction = pendingTitle.AgeRestriction,
                    ExternalLinksSerialized = pendingTitle.ExternalLinksSerialized,
                    Categories = pendingTitle.Categories,
                    Tags = pendingTitle.Tags,
                    Formats = pendingTitle.Formats,
                    Authors = pendingTitle.Authors,
                    Artists = pendingTitle.Artists,
                    Publishers = pendingTitle.Publishers,
                    Teams = pendingTitle.Teams,
                    CoverImagePath = pendingTitle.CoverImagePath,
                    BackgroundImagePath = pendingTitle.BackgroundImagePath
                };

                _context.RejectedTitles.Add(rejectedTitle);
                _context.PendingTitles.Remove(pendingTitle);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Title rejected successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting title with ID: {Id}", request.Id);
                return StatusCode(500, new { message = "Error rejecting title", error = ex.Message });
            }
        }

        /// <summary>
        /// Toggle title availability
        /// POST: api/AdminTitle/ToggleTitleAvailability
        /// </summary>
        [HttpPost("ToggleTitleAvailability")]
        public async Task<ActionResult<object>> ToggleTitleAvailability([FromBody] ToggleRequest request)
        {
            try
            {
                var title = await _context.Titles.FindAsync(request.Id);
                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                // Toggle availability
                title.IsAvailable = !title.IsAvailable;

                // Handle description modification
                if (!title.IsAvailable && !title.Description.Contains("[UNAVAILABLE]"))
                {
                    title.Description = "[UNAVAILABLE] This title is no longer available on our website. " + title.Description;
                }
                else if (title.IsAvailable && title.Description.Contains("[UNAVAILABLE]"))
                {
                    title.Description = title.Description.Replace("[UNAVAILABLE] This title is no longer available on our website. ", "");
                }

                // Create change log entry if you have this functionality
                await CreateChangeLogEntry(title.Id, "Availability Status",
                    !title.IsAvailable ? "Available" : "Unavailable",
                    title.IsAvailable ? "Available" : "Unavailable",
                    "Automatic availability change");

                await _context.SaveChangesAsync();

                return Ok(new { message = "Title availability updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling title availability for ID: {Id}", request.Id);
                return StatusCode(500, new { message = "Error updating title availability", error = ex.Message });
            }
        }

        /// <summary>
        /// Toggle title comments
        /// POST: api/AdminTitle/ToggleTitleComments
        /// </summary>
        [HttpPost("ToggleTitleComments")]
        public async Task<ActionResult<object>> ToggleTitleComments([FromBody] ToggleRequest request)
        {
            try
            {
                var title = await _context.Titles.FindAsync(request.Id);
                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                // Toggle comments
                title.AreCommentsEnabled = !title.AreCommentsEnabled;

                // Create change log entry if you have this functionality
                await CreateChangeLogEntry(title.Id, "Comments Status",
                    !title.AreCommentsEnabled ? "Enabled" : "Disabled",
                    title.AreCommentsEnabled ? "Enabled" : "Disabled",
                    "Automatic comments status change");

                await _context.SaveChangesAsync();

                return Ok(new { message = "Title comments updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling title comments for ID: {Id}", request.Id);
                return StatusCode(500, new { message = "Error updating title comments", error = ex.Message });
            }
        }

        /// <summary>
        /// Toggle chapter comments
        /// POST: api/AdminTitle/ToggleChapterComments
        /// </summary>
        [HttpPost("ToggleChapterComments")]
        public async Task<ActionResult<object>> ToggleChapterComments([FromBody] ToggleRequest request)
        {
            try
            {
                var title = await _context.Titles.FindAsync(request.Id);
                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                // Toggle chapter comments
                title.AreChapterCommentsEnabled = !title.AreChapterCommentsEnabled;

                // Create change log entry if you have this functionality
                await CreateChangeLogEntry(title.Id, "Chapter Comments Status",
                    !title.AreChapterCommentsEnabled ? "Enabled" : "Disabled",
                    title.AreChapterCommentsEnabled ? "Enabled" : "Disabled",
                    "Automatic chapter comments status change");

                await _context.SaveChangesAsync();

                return Ok(new { message = "Chapter comments updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling chapter comments for ID: {Id}", request.Id);
                return StatusCode(500, new { message = "Error updating chapter comments", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete title permanently
        /// POST: api/AdminTitle/DeleteTitle
        /// </summary>
        [HttpPost("DeleteTitle")]
        public async Task<ActionResult<object>> DeleteTitle([FromBody] DeleteTitleRequest request)
        {
            try
            {
                var title = await _context.Titles.FindAsync(request.Id);
                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                _context.Titles.Remove(title);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Title deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting title with ID: {Id}", request.Id);
                return StatusCode(500, new { message = "Error deleting title", error = ex.Message });
            }
        }

        /// <summary>
        /// Get title details for editing
        /// GET: api/AdminTitle/GetTitleDetails/{id}
        /// </summary>
        [HttpGet("GetTitleDetails/{id}")]
        [AllowAnonymous] // Override controller-level [Authorize(Roles = "Admin")]
        [Authorize] // But still require authentication
        public async Task<ActionResult<object>> GetTitleDetails(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var title = await _context.Titles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                // AUTHORIZATION CHECK: Verify user can edit this title
                var canEdit = await CanUserEditTitle(currentUser.Id, title);
                if (!canEdit)
                {
                    return Forbid("You don't have permission to edit this title");
                }

                // Rest of the method remains the same...
                var result = new
                {
                    id = title.Id,
                    originalTitle = title.OriginalTitle,
                    englishTitle = title.EnglishTitle,
                    coverImagePath = title.CoverImagePath,
                    backgroundImagePath = title.BackgroundImagePath,
                    alternativeNames = title.AlternativeNames,
                    releaseDate = title.ReleaseDate,
                    authors = title.Authors.Select(a => a.Id),
                    artists = title.Artists.Select(a => a.Id),
                    publishers = title.Publishers.Select(p => p.Id),
                    teams = title.Teams.Select(t => t.Id),
                    categories = title.Categories.Select(c => c.Id),
                    tags = title.Tags.Select(t => t.Id),
                    formats = title.Formats.Select(f => f.Id),
                    statusTitle = title.StatusTitle,
                    statusTranslation = title.StatusTranslation,
                    type = title.Type,
                    ageRestriction = title.AgeRestriction,
                    externalLinks = title.ExternalLinksSerialized,
                    description = title.Description,
                    isAvailable = title.IsAvailable,
                    areCommentsEnabled = title.AreCommentsEnabled,
                    areChapterCommentsEnabled = title.AreChapterCommentsEnabled
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching title details for ID: {Id}", id);
                return StatusCode(500, new { message = "Error fetching title details", error = ex.Message });
            }
        }


        /// <summary>
        /// Get change statistics for a title
        /// GET: api/AdminTitle/TitleChangeStats/{titleId}
        /// </summary>
        [HttpGet("TitleChangeStats/{titleId}")]
        [AllowAnonymous]
        [Authorize] // Require authentication
        public async Task<ActionResult<object>> GetTitleChangeStats(int titleId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Check if user has full access
                var hasFullAccess = await CanUserViewAllChanges(currentUser.Id, titleId);

                var query = _context.TitleChangeLogs.Where(tc => tc.TitleId == titleId);

                // Filter for regular users
                if (!hasFullAccess)
                {
                    query = query.Where(tc =>
                        tc.Status == ChangeLogStatus.Approved ||
                        tc.Status == ChangeLogStatus.AutoApproved
                    );
                }

                var totalChanges = await query.CountAsync();

                var changesByStatus = await query
                    .GroupBy(tc => tc.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                var lastUpdate = await query
                    .OrderByDescending(tc => tc.CreatedAt)
                    .Select(tc => tc.CreatedAt)
                    .FirstOrDefaultAsync();

                var stats = new
                {
                    TotalChanges = totalChanges,
                    ChangesByStatus = changesByStatus,
                    LastUpdate = lastUpdate,
                    HasFullAccess = hasFullAccess // Let frontend know what level of access user has
                };

                _logger.LogInformation(
                    "Retrieved stats for title {TitleId}: {TotalChanges} changes (FullAccess: {HasFullAccess})",
                    titleId,
                    totalChanges,
                    hasFullAccess
                );

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting title change stats for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error retrieving change statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get complete change log history for a title
        /// Public users see approved changes only, team members see everything
        /// GET: api/AdminTitle/TitleChangeLog/{titleId}
        /// </summary>
        [HttpGet("TitleChangeLog/{titleId}")]
        [AllowAnonymous]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetTitleChangeLog(int titleId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Check if user has special permissions
                var hasFullAccess = await CanUserViewAllChanges(currentUser.Id, titleId);

                // Explicitly type as IQueryable<TitleChangeLog>
                IQueryable<TitleChangeLog> query = _context.TitleChangeLogs
                    .Where(tc => tc.TitleId == titleId)
                    .Include(tc => tc.Title)
                    .Include(tc => tc.UpdatedByUser)
                    .Include(tc => tc.ReviewedByUser);

                // Filter by status if user doesn't have full access
                if (!hasFullAccess)
                {
                    query = query.Where(tc =>
                        tc.Status == ChangeLogStatus.Approved ||
                        tc.Status == ChangeLogStatus.AutoApproved
                    );
                }

                var changeLogs = await query
                    .OrderByDescending(tc => tc.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation(
                    "Loaded {Count} change logs for title {TitleId} (FullAccess: {HasFullAccess})",
                    changeLogs.Count,
                    titleId,
                    hasFullAccess
                );

                // Get all user IDs that might be missing
                var allUserIds = changeLogs
                    .SelectMany(cl => new[] { cl.UpdatedByUserId, cl.ReviewedByUserId })
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                // Load users separately as fallback
                var users = await _context.Users
                    .Where(u => allUserIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName })
                    .ToDictionaryAsync(u => u.Id, u => u.UserName);

                _logger.LogInformation("Loaded {Count} users for change logs", users.Count);

                // Map to response objects
                var result = changeLogs.Select(tc => new
                {
                    Id = tc.Id,
                    TitleId = tc.TitleId,
                    ChangeType = tc.ChangeType ?? "Unknown Change",
                    OldValue = !string.IsNullOrWhiteSpace(tc.OldValue) ? tc.OldValue : "No previous value",
                    NewValue = !string.IsNullOrWhiteSpace(tc.NewValue) ? tc.NewValue : "No new value",
                    Status = tc.Status.ToString(),
                    CreatedAt = tc.CreatedAt,
                    ReviewedAt = tc.ReviewedAt,
                    AdminComment = tc.AdminComment ?? "",
                    RejectionReason = tc.RejectionReason ?? "",
                    UpdatedByUser = new
                    {
                        Id = tc.UpdatedByUserId ?? "unknown",
                        UserName = tc.UpdatedByUser?.UserName ??
                                  (users.TryGetValue(tc.UpdatedByUserId ?? "", out var updatedUserName) ? updatedUserName : null) ??
                                  "Unknown User"
                    },
                    ReviewedByUser = !string.IsNullOrEmpty(tc.ReviewedByUserId) ? new
                    {
                        Id = tc.ReviewedByUserId,
                        UserName = tc.ReviewedByUser?.UserName ??
                                  (users.TryGetValue(tc.ReviewedByUserId, out var reviewedUserName) ? reviewedUserName : null) ??
                                  "Unknown Reviewer"
                    } : null
                }).ToList();

                _logger.LogInformation("Returning {Count} change log entries for title {TitleId}", result.Count, titleId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting title change log for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error retrieving change log", error = ex.Message });
            }
        }

        /// <summary>
        /// Check if user can view all changes (including pending/rejected)
        /// Admins and team members with edit permissions can see everything
        /// </summary>
        private async Task<bool> CanUserViewAllChanges(string userId, int titleId)
        {
            // Admins can view all change logs
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                return true;
            }

            // Check if user created the title
            var title = await _context.Titles
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == titleId);

            if (title == null)
            {
                return false;
            }

            if (title.CreatedByUserId == userId)
            {
                return true;
            }

            // Check if user has permissions in any of the title's teams
            var userTeamIds = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == userId)
                .Where(utr =>
                    // Team creators have all permissions
                    utr.Team.CreatorId == userId ||
                    // Team admins have all permissions
                    utr.Role == TeamRole.Admin ||
                    // Members with edit permissions can view all changes
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanEditTitle"))
                )
                .Select(utr => utr.TeamId)
                .ToListAsync();

            var titleTeamIds = title.Teams.Select(t => t.Id).ToList();
            return userTeamIds.Intersect(titleTeamIds).Any();
        }

        /// <summary>
        /// Helper method to check if user can view title change log
        /// </summary>
        private async Task<bool> CanUserViewTitleChangeLog(string userId, int titleId)
        {
            // Admins can view all change logs
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                return true;
            }

            // Check if user created the title
            var title = await _context.Titles
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == titleId);

            if (title == null)
            {
                return false;
            }

            if (title.CreatedByUserId == userId)
            {
                return true;
            }

            // Check if user has permissions in any of the title's teams
            var userTeamIds = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == userId)
                .Where(utr =>
                    // Team creators have all permissions
                    utr.Team.CreatorId == userId ||
                    // Team admins have all permissions
                    utr.Role == TeamRole.Admin ||
                    // Members with edit permissions can view change logs
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanEditTitle"))
                )
                .Select(utr => utr.TeamId)
                .ToListAsync();

            var titleTeamIds = title.Teams.Select(t => t.Id).ToList();
            return userTeamIds.Intersect(titleTeamIds).Any();
        }


        /// <summary>
        /// Update an existing title - UPDATED with authorization checks
        /// POST: api/AdminTitle/UpdateTitle
        /// </summary>
        [HttpPost("UpdateTitle")]
        public async Task<ActionResult<object>> UpdateTitle([FromForm] UpdateTitleRequest request)
        {
            try
            {
                _logger.LogInformation("Updating title with ID: {Id}", request.Id);

                // Find the existing title
                var existingTitle = await _context.Titles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .FirstOrDefaultAsync(t => t.Id == request.Id);

                if (existingTitle == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // AUTHORIZATION CHECK: Verify user can edit this title
                var canEdit = await CanUserEditTitle(currentUser.Id, existingTitle);
                if (!canEdit)
                {
                    return Forbid("You don't have permission to edit this title");
                }

                // If teams are being changed, verify user has permission for new teams
                if (request.Teams?.Any() == true)
                {
                    var authorizedTeamIds = await GetAuthorizedTeamIds(currentUser.Id, "EditTitles");
                    var unauthorizedTeams = request.Teams.Except(authorizedTeamIds).ToList();

                    if (unauthorizedTeams.Any())
                    {
                        var unauthorizedTeamNames = await _context.Teams
                            .Where(t => unauthorizedTeams.Contains(t.Id))
                            .Select(t => t.Name)
                            .ToListAsync();

                        return Forbid($"You don't have permission to assign this title to the following teams: {string.Join(", ", unauthorizedTeamNames)}");
                    }
                }

                // Handle image uploads
                if (request.CoverImage != null && request.CoverImage.Length > 0)
                {
                    var coverImagePath = await SaveImageAsync(request.CoverImage, "covers");
                    if (coverImagePath != null)
                    {
                        existingTitle.CoverImagePath = coverImagePath;
                    }
                }

                if (request.BackgroundImage != null && request.BackgroundImage.Length > 0)
                {
                    var backgroundImagePath = await SaveImageAsync(request.BackgroundImage, "backgrounds");
                    if (backgroundImagePath != null)
                    {
                        existingTitle.BackgroundImagePath = backgroundImagePath;
                    }
                }

                // Update basic properties
                existingTitle.OriginalTitle = request.OriginalTitle ?? string.Empty;
                existingTitle.EnglishTitle = request.EnglishTitle ?? string.Empty;
                existingTitle.AlternativeNames = request.AlternativeNames ?? string.Empty;
                existingTitle.ReleaseDate = request.ReleaseDate ?? string.Empty;
                existingTitle.Description = request.Description ?? string.Empty;
                existingTitle.StatusTitle = request.StatusTitle ?? "inproces";
                existingTitle.StatusTranslation = request.StatusTranslation ?? "inproces";
                existingTitle.Type = (MangaType)(request.Type ?? 1);
                existingTitle.AgeRestriction = request.AgeRestriction ?? 0;
                existingTitle.IsAvailable = request.IsAvailable ?? true;
                existingTitle.AreCommentsEnabled = request.AreCommentsEnabled ?? true;
                existingTitle.AreChapterCommentsEnabled = request.AreChapterCommentsEnabled ?? true;

                // Handle external links
                if (request.ExternalLinks?.Any() == true)
                {
                    var validLinks = request.ExternalLinks.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                    existingTitle.ExternalLinksSerialized = string.Join(";", validLinks);
                }
                else
                {
                    existingTitle.ExternalLinksSerialized = string.Empty;
                }

                // Handle description modifications for unavailable titles
                if (!existingTitle.IsAvailable && !existingTitle.Description.Contains("[UNAVAILABLE]"))
                {
                    existingTitle.Description = "[UNAVAILABLE] This title is no longer available on our website. " + existingTitle.Description;
                }
                else if (existingTitle.IsAvailable && existingTitle.Description.Contains("[UNAVAILABLE]"))
                {
                    existingTitle.Description = existingTitle.Description.Replace("[UNAVAILABLE] This title is no longer available on our website. ", "");
                }

                // Update many-to-many relationships
                if (request.Categories?.Any() == true)
                {
                    var categories = await _context.Set<Category>().Where(c => request.Categories.Contains(c.Id)).ToListAsync();
                    existingTitle.Categories.Clear();
                    foreach (var category in categories)
                    {
                        existingTitle.Categories.Add(category);
                    }
                }
                else
                {
                    existingTitle.Categories.Clear();
                }

                if (request.Tags?.Any() == true)
                {
                    var tags = await _context.Set<Tag>().Where(t => request.Tags.Contains(t.Id)).ToListAsync();
                    existingTitle.Tags.Clear();
                    foreach (var tag in tags)
                    {
                        existingTitle.Tags.Add(tag);
                    }
                }
                else
                {
                    existingTitle.Tags.Clear();
                }

                if (request.Formats?.Any() == true)
                {
                    var formats = await _context.Set<Format>().Where(f => request.Formats.Contains(f.Id)).ToListAsync();
                    existingTitle.Formats.Clear();
                    foreach (var format in formats)
                    {
                        existingTitle.Formats.Add(format);
                    }
                }
                else
                {
                    existingTitle.Formats.Clear();
                }

                if (request.Authors?.Any() == true)
                {
                    var authors = await _context.Set<Author>().Where(a => request.Authors.Contains(a.Id)).ToListAsync();
                    existingTitle.Authors.Clear();
                    foreach (var author in authors)
                    {
                        existingTitle.Authors.Add(author);
                    }
                }
                else
                {
                    existingTitle.Authors.Clear();
                }

                if (request.Artists?.Any() == true)
                {
                    var artists = await _context.Set<Artist>().Where(a => request.Artists.Contains(a.Id)).ToListAsync();
                    existingTitle.Artists.Clear();
                    foreach (var artist in artists)
                    {
                        existingTitle.Artists.Add(artist);
                    }
                }
                else
                {
                    existingTitle.Artists.Clear();
                }

                if (request.Publishers?.Any() == true)
                {
                    var publishers = await _context.Set<Publisher>().Where(p => request.Publishers.Contains(p.Id)).ToListAsync();
                    existingTitle.Publishers.Clear();
                    foreach (var publisher in publishers)
                    {
                        existingTitle.Publishers.Add(publisher);
                    }
                }
                else
                {
                    existingTitle.Publishers.Clear();
                }

                // Handle teams with authorization check
                if (request.Teams?.Any() == true)
                {
                    var authorizedTeamIds = await GetAuthorizedTeamIds(currentUser.Id, "EditTitles");
                    var validTeamIds = request.Teams.Intersect(authorizedTeamIds).ToList();

                    var teams = await _context.Set<Team>().Where(t => validTeamIds.Contains(t.Id)).ToListAsync();
                    existingTitle.Teams.Clear();
                    foreach (var team in teams)
                    {
                        existingTitle.Teams.Add(team);
                    }
                }
                else
                {
                    existingTitle.Teams.Clear();
                }

                // Save changes
                _context.Titles.Update(existingTitle);
                await _context.SaveChangesAsync();

                // Create change log entry
                await CreateChangeLogEntry(existingTitle.Id, "Title Update",
                    "Multiple fields updated",
                    "Title updated via admin interface",
                    "Admin title update");

                _logger.LogInformation("Title updated successfully: {EnglishTitle}", existingTitle.EnglishTitle);

                return Ok(new { message = "Title updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating title with ID: {Id}", request.Id);
                return StatusCode(500, new { message = "Error updating title", error = ex.Message });
            }
        }
        /// <summary>
        /// Check if user can edit specific title
        /// </summary>
        private async Task<bool> CanUserEditTitle(string userId, Title title)
        {
            // Admins can edit all titles
            var user = await _userManager.FindByIdAsync(userId);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                return true;
            }

            // Check if user created the title
            if (title.CreatedByUserId == userId)
            {
                return true;
            }

            // Check if user has edit permissions in any of the title's teams
            var userTeamIds = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == userId)
                .Where(utr =>
                    // Team creators have all permissions
                    utr.Team.CreatorId == userId ||
                    // Team admins have all permissions
                    utr.Role == TeamRole.Admin ||
                    // Members with specific permission - UPDATED permission name
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanEditTitle"))
                )
                .Select(utr => utr.TeamId)
                .ToListAsync();

            var titleTeamIds = title.Teams.Select(t => t.Id).ToList();
            return userTeamIds.Intersect(titleTeamIds).Any();
        }

        /// <summary>
        /// Get teams user can perform specific action on
        /// </summary>
        private async Task<List<int>> GetAuthorizedTeamIds(string userId, string permission)
        {
            // Admins can work with all teams
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    return await _context.Teams.Select(t => t.Id).ToListAsync();
                }
            }

            // Get teams where user has specific permission
            var authorizedTeamIds = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == userId)
                .Where(utr =>
                    // Team creators (owners) have all permissions
                    utr.Team.CreatorId == userId ||
                    // Team admins have all permissions
                    utr.Role == TeamRole.Admin ||
                    // Members with specific permission - UPDATED permission names
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == permission))
                )
                .Select(utr => utr.TeamId)
                .Distinct()
                .ToListAsync();

            return authorizedTeamIds;
        }


        // Add this method to AdminTitleController.cs

        /// <summary>
        /// Get all pending titles for admin review
        /// GET: api/AdminTitle/PendingTitles
        /// </summary>
        [HttpGet("PendingTitles")]
        public async Task<ActionResult<IEnumerable<object>>> GetPendingTitles()
        {
            try
            {
                var pendingTitles = await _context.PendingTitles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .Select(t => new
                    {
                        id = t.Id,
                        originalTitle = t.OriginalTitle,
                        englishTitle = t.EnglishTitle,
                        alternativeNames = t.AlternativeNames,
                        releaseDate = t.ReleaseDate,
                        description = t.Description,
                        statusTitle = t.StatusTitle,
                        statusTranslation = t.StatusTranslation,
                        type = t.Type,
                        ageRestriction = t.AgeRestriction,
                        externalLinks = t.ExternalLinksSerialized,
                        coverImagePath = t.CoverImagePath,
                        backgroundImagePath = t.BackgroundImagePath,
                        createdAt = t.CreatedAt,
                        createdByUserId = t.CreatedByUserId,
                        categories = t.Categories.Select(c => new { id = c.Id, name = c.Name }),
                        tags = t.Tags.Select(tag => new { id = tag.Id, name = tag.Name }),
                        formats = t.Formats.Select(f => new { id = f.Id, name = f.Name }),
                        authors = t.Authors.Select(a => new { id = a.Id, name = a.Name }),
                        artists = t.Artists.Select(a => new { id = a.Id, name = a.Name }),
                        publishers = t.Publishers.Select(p => new { id = p.Id, name = p.Name }),
                        teams = t.Teams.Select(team => new { id = team.Id, name = team.Name })
                    })
                    .OrderByDescending(t => t.createdAt)
                    .ToListAsync();

                return Ok(pendingTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending titles");
                return StatusCode(500, new { message = "Error fetching pending titles", error = ex.Message });
            }
        }


        /// <summary>
        /// Get pending title changes for review
        /// GET: api/AdminTitle/PendingChanges
        /// </summary>
        [HttpGet("PendingChanges")]
        public async Task<ActionResult<IEnumerable<object>>> GetPendingChanges()
        {
            try
            {
                var pendingChanges = await _context.TitleChangeLogs
                    .Where(tc => tc.Status == ChangeLogStatus.Pending)
                    .Include(tc => tc.Title)
                    .Include(tc => tc.UpdatedByUser)
                    .OrderByDescending(tc => tc.CreatedAt)
                    .GroupBy(tc => tc.TitleId)
                    .Select(g => new
                    {
                        TitleId = g.Key,
                        TitleName = g.First().Title.OriginalTitle,
                        TitleEnglishName = g.First().Title.EnglishTitle,
                        ChangeCount = g.Count(),
                        SubmittedBy = g.First().UpdatedByUser.UserName,
                        SubmittedAt = g.First().CreatedAt,
                        Changes = g.Select(tc => new
                        {
                            tc.Id,
                            tc.ChangeType,
                            tc.OldValue,
                            tc.NewValue
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(pendingChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending changes");
                return StatusCode(500, new { message = "Error fetching pending changes", error = ex.Message });
            }
        }

        /// <summary>
        /// Get pending changes for a specific title
        /// GET: api/AdminTitle/PendingChanges/{titleId}
        /// </summary>
        [HttpGet("PendingChanges/{titleId}")]
        public async Task<ActionResult<object>> GetPendingChangesForTitle(int titleId)
        {
            try
            {
                var changes = await _context.TitleChangeLogs
                    .Where(tc => tc.TitleId == titleId && tc.Status == ChangeLogStatus.Pending)
                    .Include(tc => tc.Title)
                    .Include(tc => tc.UpdatedByUser)
                    .OrderBy(tc => tc.CreatedAt)
                    .Select(tc => new
                    {
                        tc.Id,
                        tc.ChangeType,
                        tc.OldValue,
                        tc.NewValue,
                        tc.CreatedAt,
                        UpdatedBy = tc.UpdatedByUser.UserName
                    })
                    .ToListAsync();

                if (!changes.Any())
                {
                    return NotFound(new { message = "No pending changes found for this title" });
                }

                var title = await _context.Titles.FindAsync(titleId);

                return Ok(new
                {
                    TitleId = titleId,
                    TitleName = title?.OriginalTitle,
                    Changes = changes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending changes for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error fetching pending changes", error = ex.Message });
            }
        }

        /// <summary>
        /// Approve all pending changes for a title
        /// POST: api/AdminTitle/ApproveChanges/{titleId}
        /// </summary>
        [HttpPost("ApproveChanges/{titleId}")]
        public async Task<ActionResult> ApproveAllChanges(int titleId, [FromBody] ApproveChangesRequest request)
        {
            try
            {
                var adminUser = await _userManager.GetUserAsync(User);
                if (adminUser == null)
                {
                    return Unauthorized();
                }

                var pendingChanges = await _context.TitleChangeLogs
                    .Where(tc => tc.TitleId == titleId && tc.Status == ChangeLogStatus.Pending)
                    .Include(tc => tc.Title)
                        .ThenInclude(t => t.Categories)
                    .Include(tc => tc.Title)
                        .ThenInclude(t => t.Tags)
                    .Include(tc => tc.Title)
                        .ThenInclude(t => t.Formats)
                    .Include(tc => tc.Title)
                        .ThenInclude(t => t.Authors)
                    .Include(tc => tc.Title)
                        .ThenInclude(t => t.Artists)
                    .Include(tc => tc.Title)
                        .ThenInclude(t => t.Publishers)
                    .Include(tc => tc.Title)
                        .ThenInclude(t => t.Teams)
                    .ToListAsync();

                if (!pendingChanges.Any())
                {
                    return NotFound(new { message = "No pending changes found for this title" });
                }

                var title = pendingChanges.First().Title;
                var appliedChanges = new List<string>();

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var change in pendingChanges)
                    {
                        // Apply the change based on type
                        switch (change.ChangeType)
                        {
                            case "Original Title":
                                title.OriginalTitle = change.NewValue;
                                break;
                            case "English Title":
                                title.EnglishTitle = change.NewValue;
                                break;
                            case "Description":
                                title.Description = change.NewValue;
                                break;
                            case "Alternative Names":
                                title.AlternativeNames = change.NewValue;
                                break;
                            case "Release Date":
                                title.ReleaseDate = change.NewValue;
                                break;
                            case "Status":
                                title.StatusTitle = change.NewValue;
                                break;
                            case "Translation Status":
                                title.StatusTranslation = change.NewValue;
                                break;
                            case "Type":
                                if (Enum.TryParse<MangaType>(change.NewValue, out var mangaType))
                                {
                                    title.Type = mangaType;
                                }
                                break;
                            case "Age Restriction":
                                if (int.TryParse(change.NewValue, out var ageRestriction))
                                {
                                    title.AgeRestriction = ageRestriction;
                                }
                                break;
                            case "Cover Image":
                                title.CoverImagePath = change.NewValue;
                                break;
                            case "Background Image":
                                title.BackgroundImagePath = change.NewValue;
                                break;
                            case "Authors":
                                var authorIds = change.NewValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse).ToList();
                                var authors = await _context.Set<Author>().Where(a => authorIds.Contains(a.Id)).ToListAsync();
                                title.Authors.Clear();
                                foreach (var author in authors)
                                {
                                    title.Authors.Add(author);
                                }
                                break;
                            case "Artists":
                                var artistIds = change.NewValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse).ToList();
                                var artists = await _context.Set<Artist>().Where(a => artistIds.Contains(a.Id)).ToListAsync();
                                title.Artists.Clear();
                                foreach (var artist in artists)
                                {
                                    title.Artists.Add(artist);
                                }
                                break;
                            case "Publishers":
                                var publisherIds = change.NewValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse).ToList();
                                var publishers = await _context.Set<Publisher>().Where(p => publisherIds.Contains(p.Id)).ToListAsync();
                                title.Publishers.Clear();
                                foreach (var publisher in publishers)
                                {
                                    title.Publishers.Add(publisher);
                                }
                                break;
                            case "Teams":
                                var teamIds = change.NewValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse).ToList();
                                var teams = await _context.Set<Team>().Where(t => teamIds.Contains(t.Id)).ToListAsync();
                                title.Teams.Clear();
                                foreach (var team in teams)
                                {
                                    title.Teams.Add(team);
                                }
                                break;
                            case "Categories":
                                var categoryIds = change.NewValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse).ToList();
                                var categories = await _context.Set<Category>().Where(c => categoryIds.Contains(c.Id)).ToListAsync();
                                title.Categories.Clear();
                                foreach (var category in categories)
                                {
                                    title.Categories.Add(category);
                                }
                                break;
                            case "Tags":
                                var tagIds = change.NewValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse).ToList();
                                var tags = await _context.Set<Tag>().Where(t => tagIds.Contains(t.Id)).ToListAsync();
                                title.Tags.Clear();
                                foreach (var tag in tags)
                                {
                                    title.Tags.Add(tag);
                                }
                                break;
                            case "Formats":
                                var formatIds = change.NewValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse).ToList();
                                var formats = await _context.Set<Format>().Where(f => formatIds.Contains(f.Id)).ToListAsync();
                                title.Formats.Clear();
                                foreach (var format in formats)
                                {
                                    title.Formats.Add(format);
                                }
                                break;
                            case "External Links":
                                title.ExternalLinksSerialized = change.NewValue;
                                break;
                        }

                        // Update change log status
                        change.Status = ChangeLogStatus.Approved;
                        change.ReviewedByUserId = adminUser.Id;
                        change.ReviewedAt = DateTime.UtcNow;
                        change.AdminComment = request?.AdminComment ?? "";

                        // Create approved change record
                        var approvedChange = new ApprovedTitleChange
                        {
                            TitleId = titleId,
                            UpdatedByUserId = change.UpdatedByUserId,
                            ReviewedByUserId = adminUser.Id,
                            CreatedAt = change.CreatedAt,
                            ApprovedAt = DateTime.UtcNow,
                            ChangeType = change.ChangeType,
                            OldValue = change.OldValue,
                            NewValue = change.NewValue,
                            AdminComment = request?.AdminComment ?? "",
                            IsAutoApproved = false
                        };

                        _context.ApprovedTitleChanges.Add(approvedChange);
                        appliedChanges.Add(change.ChangeType);
                    }

                    _context.Titles.Update(title);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Approved {ChangeCount} changes for title {TitleId} by admin {AdminId}",
                        appliedChanges.Count, titleId, adminUser.Id);

                    return Ok(new
                    {
                        message = $"Successfully approved {appliedChanges.Count} changes",
                        appliedChanges = appliedChanges,
                        titleId = titleId
                    });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving changes for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error approving changes", error = ex.Message });
            }
        }

        /// <summary>
        /// Reject all pending changes for a title
        /// POST: api/AdminTitle/RejectChanges/{titleId}
        /// </summary>
        [HttpPost("RejectChanges/{titleId}")]
        public async Task<ActionResult> RejectAllChanges(int titleId, [FromBody] RejectChangesRequest request)
        {
            try
            {
                var adminUser = await _userManager.GetUserAsync(User);
                if (adminUser == null)
                {
                    return Unauthorized();
                }

                var pendingChanges = await _context.TitleChangeLogs
                    .Where(tc => tc.TitleId == titleId && tc.Status == ChangeLogStatus.Pending)
                    .ToListAsync();

                if (!pendingChanges.Any())
                {
                    return NotFound(new { message = "No pending changes found for this title" });
                }

                foreach (var change in pendingChanges)
                {
                    change.Status = ChangeLogStatus.Rejected;
                    change.ReviewedByUserId = adminUser.Id;
                    change.ReviewedAt = DateTime.UtcNow;
                    change.RejectionReason = request.RejectionReason ?? "Changes not approved";
                    change.AdminComment = request.AdminComment ?? "";

                    // Create rejected change record
                    var rejectedChange = new RejectedTitleChange
                    {
                        TitleId = titleId,
                        UpdatedByUserId = change.UpdatedByUserId,
                        ReviewedByUserId = adminUser.Id,
                        CreatedAt = change.CreatedAt,
                        RejectedAt = DateTime.UtcNow,
                        ChangeType = change.ChangeType,
                        OldValue = change.OldValue,
                        NewValue = change.NewValue,
                        AdminComment = request.AdminComment ?? "",
                        RejectionReason = request.RejectionReason ?? "Changes not approved"
                    };

                    _context.RejectedTitleChanges.Add(rejectedChange);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Rejected {ChangeCount} changes for title {TitleId} by admin {AdminId}",
                    pendingChanges.Count, titleId, adminUser.Id);

                return Ok(new
                {
                    message = $"Successfully rejected {pendingChanges.Count} changes",
                    rejectedCount = pendingChanges.Count,
                    titleId = titleId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting changes for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error rejecting changes", error = ex.Message });
            }
        }

        // Request models
        public class ApproveChangesRequest
        {
            public string? AdminComment { get; set; }
        }

        public class RejectChangesRequest
        {
            public string RejectionReason { get; set; } = "Changes not approved";
            public string? AdminComment { get; set; }
        }

        public class UpdateTitleRequest
        {
            public int Id { get; set; }
            public string? OriginalTitle { get; set; }
            public string? EnglishTitle { get; set; }
            public string? AlternativeNames { get; set; }
            public string? ReleaseDate { get; set; }
            public string? Description { get; set; }
            public string? StatusTitle { get; set; }
            public string? StatusTranslation { get; set; }
            public int? Type { get; set; }
            public int? AgeRestriction { get; set; }
            public bool? IsAvailable { get; set; }
            public bool? AreCommentsEnabled { get; set; }
            public bool? AreChapterCommentsEnabled { get; set; }

            // File uploads
            public IFormFile? CoverImage { get; set; }
            public IFormFile? BackgroundImage { get; set; }

            // Arrays for many-to-many relationships
            public List<int>? Authors { get; set; }
            public List<int>? Artists { get; set; }
            public List<int>? Publishers { get; set; }
            public List<int>? Teams { get; set; }
            public List<int>? Categories { get; set; }
            public List<int>? Tags { get; set; }
            public List<int>? Formats { get; set; }

            // External links
            public List<string>? ExternalLinks { get; set; }
        }
        // Add this method to your AdminTitleController.cs class

        /// <summary>
        /// Helper method to save uploaded images
        /// </summary>
        private async Task<string?> SaveImageAsync(IFormFile image, string folder)
        {
            if (image == null || image.Length == 0)
                return null;

            try
            {
                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    _logger.LogWarning("Invalid file type: {ContentType}", image.ContentType);
                    return null;
                }

                // Validate file size (5MB limit)
                if (image.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("File too large: {Size} bytes", image.Length);
                    return null;
                }

                var uploads = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", folder);

                // Ensure directory exists
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                    _logger.LogDebug("Created directory: {Path}", uploads);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(image.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploads, fileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                var relativePath = $"/uploads/{folder}/{fileName}";
                _logger.LogDebug("Image saved successfully: {Path}", relativePath);

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image to {Folder}", folder);
                return null;
            }
        }

        /// <summary>
        /// Search titles by string
        /// GET: api/AdminTitle/SearchTitle?searchString={searchString}
        /// </summary>
        [HttpGet("SearchTitle")]
        public async Task<ActionResult<IEnumerable<object>>> SearchTitle(string searchString)
        {
            try
            {
                var query = _context.Titles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(s => s.OriginalTitle.Contains(searchString) || s.EnglishTitle.Contains(searchString));
                }

                var titles = await query
                    .Select(t => new
                    {
                        id = t.Id,
                        originalTitle = t.OriginalTitle,
                        englishTitle = t.EnglishTitle,
                        type = t.Type,
                        isAvailable = t.IsAvailable,
                        areCommentsEnabled = t.AreCommentsEnabled,
                        areChapterCommentsEnabled = t.AreChapterCommentsEnabled
                    })
                    .ToListAsync();

                return Ok(titles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching titles with query: {SearchString}", searchString);
                return StatusCode(500, new { message = "Error searching titles", error = ex.Message });
            }
        }

        /// <summary>
        /// Helper method to create change log entries (optional - implement if you have change logging)
        /// </summary>
        private async Task CreateChangeLogEntry(int titleId, string changeType, string oldValue, string newValue, string adminComment)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // Validate user exists
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Cannot create change log entry - user not authenticated");
                    return;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot create change log entry - user {UserId} not found", userId);
                    return;
                }

                if (_context.Model.FindEntityType(typeof(TitleChangeLog)) != null)
                {
                    var changeLog = new TitleChangeLog
                    {
                        TitleId = titleId,
                        ChangeType = changeType,
                        OldValue = oldValue ?? "",  // Ensure not null
                        NewValue = newValue ?? "",  // Ensure not null
                        CreatedAt = DateTime.UtcNow,
                        UpdatedByUserId = userId,
                        ReviewedByUserId = userId,
                        ReviewedAt = DateTime.UtcNow,
                        AdminComment = adminComment ?? "",
                        RejectionReason = string.Empty,
                        Status = ChangeLogStatus.Approved
                    };

                    _context.TitleChangeLogs.Add(changeLog);
                    await _context.SaveChangesAsync(); // Save immediately to ensure it's persisted
                }

                // Also save to ApprovedTitleChange table
                if (_context.Model.FindEntityType(typeof(ApprovedTitleChange)) != null)
                {
                    var approvedChange = new ApprovedTitleChange
                    {
                        TitleId = titleId,
                        UpdatedByUserId = userId,
                        ReviewedByUserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        ApprovedAt = DateTime.UtcNow,
                        ChangeType = changeType,
                        OldValue = oldValue ?? "",
                        NewValue = newValue ?? "",
                        AdminComment = adminComment ?? "",
                        IsAutoApproved = true
                    };

                    _context.ApprovedTitleChanges.Add(approvedChange);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating change log entry for title {TitleId}", titleId);
                // Don't throw - this is optional functionality
            }
        }

        // Request DTOs
        public class AdminRejectTitleRequest
        {
            public int Id { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        public class ToggleRequest
        {
            public int Id { get; set; }
        }

        public class DeleteTitleRequest
        {
            public int Id { get; set; }
        }
    }
}