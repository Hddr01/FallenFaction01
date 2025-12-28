using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using System.ComponentModel.DataAnnotations;
using static FallenFaction.Server.Controllers.Api.AdminTitleController;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all actions
    public class TitleApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<TitleApiController> _logger;

        public TitleApiController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IWebHostEnvironment hostingEnvironment,
            ILogger<TitleApiController> logger)
        {
            _context = context;
            _userManager = userManager;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        // GET: api/TitleApi/form-data - UPDATED with team filtering
        [HttpGet("form-data")]
        public async Task<IActionResult> GetFormData()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // Get all teams where the user is a member with appropriate permissions
                var userTeams = await _context.UserTeamRoles
                    .Where(utr => utr.AppUserId == user.Id)
                    .Include(utr => utr.Team)
                    .Include(utr => utr.UserTeamRolePermissions)
                        .ThenInclude(utrp => utrp.UserTeamPermission)
                    .Where(utr =>
                        // Team creators (owners) can always add titles
                        utr.Team.CreatorId == user.Id ||
                        // Admins can add titles
                        utr.Role == TeamRole.Admin ||
                        // Members with specific permissions can add titles
                        (utr.Role == TeamRole.Member &&
                         utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanAddTitle"))
                    )
                    .Select(utr => new { utr.Team.Id, utr.Team.Name })
                    .Distinct()
                    .ToListAsync();

                var formData = new
                {
                    Authors = await _context.Set<Author>().Select(a => new { a.Id, a.Name }).ToListAsync(),
                    Artists = await _context.Set<Artist>().Select(a => new { a.Id, a.Name }).ToListAsync(),
                    Publishers = await _context.Set<Publisher>().Select(p => new { p.Id, p.Name }).ToListAsync(),
                    Teams = userTeams, // Only teams user can add titles to
                    Categories = await _context.Set<Category>().Select(c => new { c.Id, c.Name }).ToListAsync(),
                    Tags = await _context.Set<Tag>().Select(t => new { t.Id, t.Name }).ToListAsync(),
                    Formats = await _context.Set<Format>().Select(f => new { f.Id, f.Name }).ToListAsync()
                };

                return Ok(formData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading form data");
                return StatusCode(500, new { error = "Failed to load form data" });
            }
        }

        // POST: api/TitleApi/create - UPDATED with team validation
        [HttpPost("create")]
        public async Task<IActionResult> CreatePendingTitle([FromForm] CreateTitleRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.EnglishTitle))
                {
                    return BadRequest(new { error = "English title is required" });
                }

                if (request.Teams == null || !request.Teams.Any())
                {
                    return BadRequest(new { error = "At least one team must be selected" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // AUTHORIZATION CHECK: Verify user can add titles to all selected teams
                var authorizedTeamIds = await GetAuthorizedTeamIds(user.Id, "CanAddTitle");
                var unauthorizedTeams = request.Teams.Except(authorizedTeamIds).ToList();

                if (unauthorizedTeams.Any())
                {
                    var unauthorizedTeamNames = await _context.Teams
                        .Where(t => unauthorizedTeams.Contains(t.Id))
                        .Select(t => t.Name)
                        .ToListAsync();

                    return Forbid($"You don't have permission to add titles to the following teams: {string.Join(", ", unauthorizedTeamNames)}");
                }

                // Verify all selected teams exist
                var selectedTeams = await _context.Teams
                    .Where(t => request.Teams.Contains(t.Id))
                    .ToListAsync();

                if (selectedTeams.Count != request.Teams.Count)
                {
                    return BadRequest(new { error = "One or more selected teams do not exist" });
                }

                // Create the pending title
                var pendingTitle = new PendingTitle
                {
                    OriginalTitle = request.OriginalTitle ?? string.Empty,
                    EnglishTitle = request.EnglishTitle,
                    AlternativeNames = request.AlternativeNames ?? string.Empty,
                    ReleaseDate = request.ReleaseDate ?? string.Empty,
                    Description = request.Description ?? string.Empty,
                    StatusTitle = request.StatusTitle ?? "inproces",
                    StatusTranslation = request.StatusTranslation ?? "inproces",
                    Type = (MangaType)(request.Type ?? 1),
                    AgeRestriction = request.AgeRestriction ?? 0,
                    CreatedByUserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExternalLinksSerialized = string.Join(";", request.ExternalLinks ?? new List<string>())
                };

                // Handle image uploads
                if (request.CoverImage != null)
                {
                    var coverImagePath = await SaveImageAsync(request.CoverImage, "covers");
                    if (coverImagePath != null)
                    {
                        pendingTitle.CoverImagePath = coverImagePath;
                    }
                }

                if (request.BackgroundImage != null)
                {
                    var backgroundImagePath = await SaveImageAsync(request.BackgroundImage, "backgrounds");
                    if (backgroundImagePath != null)
                    {
                        pendingTitle.BackgroundImagePath = backgroundImagePath;
                    }
                }

                // Add to context
                _context.Set<PendingTitle>().Add(pendingTitle);
                await _context.SaveChangesAsync();

                // Handle many-to-many relationships after saving to get the ID
                await UpdatePendingTitleCollections(pendingTitle, request);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Pending title created successfully: {pendingTitle.EnglishTitle} by user {user.UserName} for teams: {string.Join(", ", selectedTeams.Select(t => t.Name))}");

                return Ok(new
                {
                    message = "Title submitted for review successfully!",
                    titleId = pendingTitle.Id,
                    englishTitle = pendingTitle.EnglishTitle
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pending title");
                return StatusCode(500, new { error = "Failed to create title: " + ex.Message });
            }
        }

        /// <summary>
        /// Submit title edit for approval - UPDATED to use TitleChangeLog
        /// POST: api/TitleApi/edit/{id}
        /// </summary>
        [HttpPost("edit/{id}")]
        public async Task<IActionResult> SubmitTitleEdit(int id, [FromForm] UpdateTitleRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var existingTitle = await _context.Titles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (existingTitle == null)
                {
                    return NotFound(new { error = "Title not found" });
                }

                // Check if user can edit this title
                if (!await CanUserEditTitle(user.Id, id))
                {
                    return Forbid("You don't have permission to edit this title");
                }

                // If teams are being changed, verify user has permission for new teams
                if (request.Teams?.Any() == true)
                {
                    var authorizedTeamIds = await GetAuthorizedTeamIds(user.Id, "CanEditTitle");
                    var unauthorizedTeams = request.Teams.Except(authorizedTeamIds).ToList();

                    if (unauthorizedTeams.Any())
                    {
                        var unauthorizedTeamNames = await _context.Teams
                            .Where(t => unauthorizedTeams.Contains(t.Id))
                            .Select(t => t.Name)
                            .ToListAsync();

                        return Forbid($"You don't have permission to assign this title to: {string.Join(", ", unauthorizedTeamNames)}");
                    }
                }

                // Create change logs for each field that changed
                var changeLogs = new List<TitleChangeLog>();

                // Basic fields
                if (existingTitle.OriginalTitle != request.OriginalTitle)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Original Title",
                        OldValue = existingTitle.OriginalTitle ?? "",
                        NewValue = request.OriginalTitle ?? "",
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if (existingTitle.EnglishTitle != request.EnglishTitle)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "English Title",
                        OldValue = existingTitle.EnglishTitle ?? "",
                        NewValue = request.EnglishTitle ?? "",
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if (existingTitle.Description != request.Description)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Description",
                        OldValue = existingTitle.Description?.Substring(0, Math.Min(100, existingTitle.Description?.Length ?? 0)) ?? "",
                        NewValue = request.Description?.Substring(0, Math.Min(100, request.Description?.Length ?? 0)) ?? "",
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if (existingTitle.AlternativeNames != request.AlternativeNames)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Alternative Names",
                        OldValue = existingTitle.AlternativeNames ?? "",
                        NewValue = request.AlternativeNames ?? "",
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if (existingTitle.ReleaseDate != request.ReleaseDate)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Release Date",
                        OldValue = existingTitle.ReleaseDate ?? "",
                        NewValue = request.ReleaseDate ?? "",
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if (existingTitle.StatusTitle != request.StatusTitle)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Status",
                        OldValue = existingTitle.StatusTitle ?? "",
                        NewValue = request.StatusTitle ?? "",
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if (existingTitle.StatusTranslation != request.StatusTranslation)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Translation Status",
                        OldValue = existingTitle.StatusTranslation ?? "",
                        NewValue = request.StatusTranslation ?? "",
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if ((int)existingTitle.Type != request.Type)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Type",
                        OldValue = existingTitle.Type.ToString(),
                        NewValue = ((MangaType)request.Type.Value).ToString(),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if (existingTitle.AgeRestriction != request.AgeRestriction)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Age Restriction",
                        OldValue = existingTitle.AgeRestriction.ToString(),
                        NewValue = request.AgeRestriction.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                // Handle image uploads
                if (request.CoverImage != null && request.CoverImage.Length > 0)
                {
                    var coverImagePath = await SaveImageAsync(request.CoverImage, "covers");
                    if (coverImagePath != null)
                    {
                        changeLogs.Add(new TitleChangeLog
                        {
                            TitleId = id,
                            UpdatedByUserId = user.Id,
                            ChangeType = "Cover Image",
                            OldValue = existingTitle.CoverImagePath ?? "",
                            NewValue = coverImagePath,
                            CreatedAt = DateTime.UtcNow,
                            Status = ChangeLogStatus.Pending,
                            AdminComment = "",
                            RejectionReason = ""
                        });
                    }
                }

                if (request.BackgroundImage != null && request.BackgroundImage.Length > 0)
                {
                    var backgroundImagePath = await SaveImageAsync(request.BackgroundImage, "backgrounds");
                    if (backgroundImagePath != null)
                    {
                        changeLogs.Add(new TitleChangeLog
                        {
                            TitleId = id,
                            UpdatedByUserId = user.Id,
                            ChangeType = "Background Image",
                            OldValue = existingTitle.BackgroundImagePath ?? "",
                            NewValue = backgroundImagePath,
                            CreatedAt = DateTime.UtcNow,
                            Status = ChangeLogStatus.Pending,
                            AdminComment = "",
                            RejectionReason = ""
                        });
                    }
                }

                // Check for changes in relationships (serialize as comma-separated IDs)
                var existingAuthorIds = existingTitle.Authors.Select(a => a.Id).OrderBy(x => x).ToList();
                var newAuthorIds = (request.Authors ?? new List<int>()).OrderBy(x => x).ToList();
                if (!existingAuthorIds.SequenceEqual(newAuthorIds))
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Authors",
                        OldValue = string.Join(",", existingAuthorIds),
                        NewValue = string.Join(",", newAuthorIds),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                var existingArtistIds = existingTitle.Artists.Select(a => a.Id).OrderBy(x => x).ToList();
                var newArtistIds = (request.Artists ?? new List<int>()).OrderBy(x => x).ToList();
                if (!existingArtistIds.SequenceEqual(newArtistIds))
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Artists",
                        OldValue = string.Join(",", existingArtistIds),
                        NewValue = string.Join(",", newArtistIds),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                var existingPublisherIds = existingTitle.Publishers.Select(p => p.Id).OrderBy(x => x).ToList();
                var newPublisherIds = (request.Publishers ?? new List<int>()).OrderBy(x => x).ToList();
                if (!existingPublisherIds.SequenceEqual(newPublisherIds))
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Publishers",
                        OldValue = string.Join(",", existingPublisherIds),
                        NewValue = string.Join(",", newPublisherIds),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                var existingTeamIds = existingTitle.Teams.Select(t => t.Id).OrderBy(x => x).ToList();
                var newTeamIds = (request.Teams ?? new List<int>()).OrderBy(x => x).ToList();
                if (!existingTeamIds.SequenceEqual(newTeamIds))
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Teams",
                        OldValue = string.Join(",", existingTeamIds),
                        NewValue = string.Join(",", newTeamIds),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                var existingCategoryIds = existingTitle.Categories.Select(c => c.Id).OrderBy(x => x).ToList();
                var newCategoryIds = (request.Categories ?? new List<int>()).OrderBy(x => x).ToList();
                if (!existingCategoryIds.SequenceEqual(newCategoryIds))
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Categories",
                        OldValue = string.Join(",", existingCategoryIds),
                        NewValue = string.Join(",", newCategoryIds),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                var existingTagIds = existingTitle.Tags.Select(t => t.Id).OrderBy(x => x).ToList();
                var newTagIds = (request.Tags ?? new List<int>()).OrderBy(x => x).ToList();
                if (!existingTagIds.SequenceEqual(newTagIds))
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Tags",
                        OldValue = string.Join(",", existingTagIds),
                        NewValue = string.Join(",", newTagIds),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                var existingFormatIds = existingTitle.Formats.Select(f => f.Id).OrderBy(x => x).ToList();
                var newFormatIds = (request.Formats ?? new List<int>()).OrderBy(x => x).ToList();
                if (!existingFormatIds.SequenceEqual(newFormatIds))
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "Formats",
                        OldValue = string.Join(",", existingFormatIds),
                        NewValue = string.Join(",", newFormatIds),
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                // External links
                var existingLinks = existingTitle.ExternalLinksSerialized ?? "";
                var newLinks = string.Join(";", request.ExternalLinks?.Where(l => !string.IsNullOrWhiteSpace(l)) ?? new List<string>());
                if (existingLinks != newLinks)
                {
                    changeLogs.Add(new TitleChangeLog
                    {
                        TitleId = id,
                        UpdatedByUserId = user.Id,
                        ChangeType = "External Links",
                        OldValue = existingLinks,
                        NewValue = newLinks,
                        CreatedAt = DateTime.UtcNow,
                        Status = ChangeLogStatus.Pending,
                        AdminComment = "",
                        RejectionReason = ""
                    });
                }

                if (!changeLogs.Any())
                {
                    return BadRequest(new { error = "No changes detected" });
                }

                // Save all change logs with Pending status
                _context.TitleChangeLogs.AddRange(changeLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Title edit submitted for approval: {TitleId} by user {UserId}, {ChangeCount} changes",
                    id, user.Id, changeLogs.Count);

                return Ok(new
                {
                    message = "Changes submitted for admin approval!",
                    changeCount = changeLogs.Count,
                    titleId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting title edit for {TitleId}", id);
                return StatusCode(500, new { error = "Failed to submit changes: " + ex.Message });
            }
        }




        // HELPER METHOD: Get teams user can perform specific action on
        private async Task<List<int>> GetAuthorizedTeamIds(string userId, string permission)
        {
            // Admins can work with all teams
            var isAdmin = await _userManager.IsInRoleAsync(await _userManager.FindByIdAsync(userId), "Admin");
            if (isAdmin)
            {
                return await _context.Teams.Select(t => t.Id).ToListAsync();
            }

            // Get teams where user has specific permission
            var authorizedTeamIds = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == userId)
                .Where(utr =>
                    // Team creators (owners) have all permissions
                    utr.Team.CreatorId == userId ||
                    // Team admins have all permissions
                    utr.Role == TeamRole.Admin ||
                    // Members with specific permission
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == permission))
                )
                .Select(utr => utr.TeamId)
                .Distinct()
                .ToListAsync();

            return authorizedTeamIds;
        }

        // HELPER METHOD: Check if user can edit specific title
        private async Task<bool> CanUserEditTitle(string userId, int titleId)
        {
            // Admins can edit all titles
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

            // Check if user has edit permissions in any of the title's teams
            var userTeamIds = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == userId)
                .Where(utr =>
                    utr.Role == TeamRole.Admin ||
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanEditTitle"))
                )
                .Select(utr => utr.TeamId)
                .ToListAsync();

            var titleTeamIds = title.Teams.Select(t => t.Id).ToList();
            return userTeamIds.Intersect(titleTeamIds).Any();
        }

        // GET: api/TitleApi/user-pending - UPDATED with proper filtering
        [HttpGet("user-pending")]
        public async Task<IActionResult> GetUserPendingTitles()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var pendingTitles = await _context.Set<PendingTitle>()
                    .Where(t => t.CreatedByUserId == user.Id)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Categories)
                    .Include(t => t.Teams)
                    .Select(t => new
                    {
                        t.Id,
                        t.EnglishTitle,
                        t.OriginalTitle,
                        t.CoverImagePath,
                        t.Type,
                        t.Description,
                        t.CreatedAt,
                        Authors = t.Authors.Select(a => new { a.Id, a.Name }),
                        Artists = t.Artists.Select(a => new { a.Id, a.Name }),
                        Categories = t.Categories.Select(c => new { c.Id, c.Name }),
                        Teams = t.Teams.Select(team => new { team.Id, team.Name })
                    })
                    .ToListAsync();

                return Ok(pendingTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user pending titles");
                return StatusCode(500, new { error = "Failed to load pending titles" });
            }
        }

        // GET: api/TitleApi/user-rejected - UPDATED with proper filtering
        [HttpGet("user-rejected")]
        public async Task<IActionResult> GetUserRejectedTitles()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var rejectedTitles = await _context.Set<RejectedTitle>()
                    .Where(t => t.CreatedByUserId == user.Id)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Categories)
                    .Include(t => t.Teams)
                    .Select(t => new
                    {
                        t.Id,
                        t.EnglishTitle,
                        t.OriginalTitle,
                        t.CoverImagePath,
                        t.Type,
                        t.Description,
                        t.RejectedAt,
                        t.RejectionReason,
                        Authors = t.Authors.Select(a => new { a.Id, a.Name }),
                        Artists = t.Artists.Select(a => new { a.Id, a.Name }),
                        Categories = t.Categories.Select(c => new { c.Id, c.Name }),
                        Teams = t.Teams.Select(team => new { team.Id, team.Name })
                    })
                    .ToListAsync();

                return Ok(rejectedTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user rejected titles");
                return StatusCode(500, new { error = "Failed to load rejected titles" });
            }
        }

        // Existing helper methods remain unchanged...
        private async Task UpdatePendingTitleCollections(PendingTitle pendingTitle, CreateTitleRequest request)
        {
            // Load the pending title with its collections
            var titleWithCollections = await _context.Set<PendingTitle>()
                .Include(t => t.Authors)
                .Include(t => t.Artists)
                .Include(t => t.Publishers)
                .Include(t => t.Teams)
                .Include(t => t.Categories)
                .Include(t => t.Tags)
                .Include(t => t.Formats)
                .FirstOrDefaultAsync(t => t.Id == pendingTitle.Id);

            if (titleWithCollections == null) return;

            // Update collections - only allow teams user has permission for
            if (request.Authors?.Any() == true)
            {
                var authors = await _context.Set<Author>().Where(a => request.Authors.Contains(a.Id)).ToListAsync();
                titleWithCollections.Authors = authors;
            }

            if (request.Artists?.Any() == true)
            {
                var artists = await _context.Set<Artist>().Where(a => request.Artists.Contains(a.Id)).ToListAsync();
                titleWithCollections.Artists = artists;
            }

            if (request.Publishers?.Any() == true)
            {
                var publishers = await _context.Set<Publisher>().Where(p => request.Publishers.Contains(p.Id)).ToListAsync();
                titleWithCollections.Publishers = publishers;
            }

            if (request.Teams?.Any() == true)
            {
                // Double-check team authorization again
                var user = await _userManager.GetUserAsync(User);
                var authorizedTeamIds = await GetAuthorizedTeamIds(user.Id, "CanAddTitle");
                var validTeamIds = request.Teams.Intersect(authorizedTeamIds).ToList();

                var teams = await _context.Set<Team>().Where(t => validTeamIds.Contains(t.Id)).ToListAsync();
                titleWithCollections.Teams = teams;
            }

            if (request.Categories?.Any() == true)
            {
                var categories = await _context.Set<Category>().Where(c => request.Categories.Contains(c.Id)).ToListAsync();
                titleWithCollections.Categories = categories;
            }

            if (request.Tags?.Any() == true)
            {
                var tags = await _context.Set<Tag>().Where(t => request.Tags.Contains(t.Id)).ToListAsync();
                titleWithCollections.Tags = tags;
            }

            if (request.Formats?.Any() == true)
            {
                var formats = await _context.Set<Format>().Where(f => request.Formats.Contains(f.Id)).ToListAsync();
                titleWithCollections.Formats = formats;
            }
        }

        private async Task<string?> SaveImageAsync(IFormFile image, string folder)
        {
            if (image == null || image.Length == 0)
                return null;

            try
            {
                var uploads = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", folder);
                Directory.CreateDirectory(uploads);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploads, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                return $"/uploads/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving image to {folder}");
                return null;
            }
        }

        // POST: api/TitleApi/approve/{id} - Admin only, already properly secured
        [HttpPost("approve/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePendingTitle(int id)
        {
            // Existing implementation remains the same...
            try
            {
                var pendingTitle = await _context.Set<PendingTitle>()
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (pendingTitle == null)
                {
                    return NotFound(new { error = "Pending title not found" });
                }

                // Validate that CreatedByUserId exists
                if (string.IsNullOrEmpty(pendingTitle.CreatedByUserId))
                {
                    return BadRequest(new { error = "Pending title has no associated creator" });
                }

                // Create the actual title
                var title = new Title
                {
                    OriginalTitle = pendingTitle.OriginalTitle,
                    EnglishTitle = pendingTitle.EnglishTitle,
                    AlternativeNames = pendingTitle.AlternativeNames,
                    ReleaseDate = pendingTitle.ReleaseDate,
                    Description = pendingTitle.Description,
                    StatusTitle = pendingTitle.StatusTitle,
                    StatusTranslation = pendingTitle.StatusTranslation,
                    Type = pendingTitle.Type,
                    AgeRestriction = pendingTitle.AgeRestriction,
                    CoverImagePath = pendingTitle.CoverImagePath,
                    BackgroundImagePath = pendingTitle.BackgroundImagePath,
                    ExternalLinksSerialized = pendingTitle.ExternalLinksSerialized,
                    CreatedByUserId = pendingTitle.CreatedByUserId,
                    CreatedAt = DateTime.UtcNow,
                    // Copy the relationship collections
                    Authors = pendingTitle.Authors,
                    Artists = pendingTitle.Artists,
                    Publishers = pendingTitle.Publishers,
                    Teams = pendingTitle.Teams,
                    Categories = pendingTitle.Categories,
                    Tags = pendingTitle.Tags,
                    Formats = pendingTitle.Formats
                };

                _context.Set<Title>().Add(title);
                _context.Set<PendingTitle>().Remove(pendingTitle);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Pending title approved and converted to title: {title.EnglishTitle}");

                return Ok(new
                {
                    message = "Title approved successfully!",
                    titleId = title.Id,
                    englishTitle = title.EnglishTitle
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving pending title {id}");
                return StatusCode(500, new { error = "Failed to approve title: " + ex.Message });
            }
        }

        // POST: api/TitleApi/reject/{id} - Admin only, already properly secured
        [HttpPost("reject/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectPendingTitle(int id, [FromBody] RejectTitleRequest request)
        {
            // Existing implementation remains the same...
            try
            {
                var pendingTitle = await _context.Set<PendingTitle>()
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (pendingTitle == null)
                {
                    return NotFound(new { error = "Pending title not found" });
                }

                // Create rejected title
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
                    CoverImagePath = pendingTitle.CoverImagePath,
                    BackgroundImagePath = pendingTitle.BackgroundImagePath,
                    ExternalLinksSerialized = pendingTitle.ExternalLinksSerialized,
                    CreatedByUserId = pendingTitle.CreatedByUserId,
                    CreatedAt = pendingTitle.CreatedAt,
                    RejectedAt = DateTime.UtcNow,
                    RejectionReason = request.Reason ?? "No reason provided",
                    // Copy the relationship collections
                    Authors = pendingTitle.Authors,
                    Artists = pendingTitle.Artists,
                    Publishers = pendingTitle.Publishers,
                    Teams = pendingTitle.Teams,
                    Categories = pendingTitle.Categories,
                    Tags = pendingTitle.Tags,
                    Formats = pendingTitle.Formats
                };

                _context.Set<RejectedTitle>().Add(rejectedTitle);
                _context.Set<PendingTitle>().Remove(pendingTitle);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Pending title rejected: {rejectedTitle.EnglishTitle}, Reason: {request.Reason}");

                return Ok(new
                {
                    message = "Title rejected successfully!",
                    rejectedTitleId = rejectedTitle.Id,
                    englishTitle = rejectedTitle.EnglishTitle,
                    rejectionReason = rejectedTitle.RejectionReason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting pending title {id}");
                return StatusCode(500, new { error = "Failed to reject title: " + ex.Message });
            }
        }
    }

    // Request model remains the same
    public class CreateTitleRequest
    {
        [Required]
        public string EnglishTitle { get; set; } = string.Empty;

        public string? OriginalTitle { get; set; }
        public string? AlternativeNames { get; set; }
        public int? Type { get; set; }
        public string? ReleaseDate { get; set; }
        public string? Description { get; set; }
        public string? StatusTitle { get; set; }
        public string? StatusTranslation { get; set; }
        public int? AgeRestriction { get; set; }
        public List<string>? ExternalLinks { get; set; }

        // File uploads
        public IFormFile? CoverImage { get; set; }
        public IFormFile? BackgroundImage { get; set; }

        // Many-to-many relationships (arrays of IDs)
        public List<int>? Authors { get; set; }
        public List<int>? Artists { get; set; }
        public List<int>? Publishers { get; set; }
        public List<int>? Teams { get; set; }
        public List<int>? Categories { get; set; }
        public List<int>? Tags { get; set; }
        public List<int>? Formats { get; set; }
    }

    // Request model for rejecting titles
    public class RejectTitleRequest
    {
        public string? Reason { get; set; }
    }
}