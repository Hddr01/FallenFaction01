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
        public async Task<ActionResult<object>> RejectTitle([FromBody] RejectTitleRequest request)
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
        public async Task<ActionResult<object>> GetTitleDetails(int id)
        {
            try
            {
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
        // Add this method to your AdminTitleController.cs

        /// <summary>
        /// Update an existing title
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

                if (request.Teams?.Any() == true)
                {
                    var teams = await _context.Set<Team>().Where(t => request.Teams.Contains(t.Id)).ToListAsync();
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

        // Add this request model class at the bottom of your AdminTitleController.cs file
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

                // Only create change log if the tables exist in your database
                if (_context.Model.FindEntityType(typeof(TitleChangeLog)) != null)
                {
                    var changeLog = new TitleChangeLog
                    {
                        TitleId = titleId,
                        ChangeType = changeType,
                        OldValue = oldValue,
                        NewValue = newValue,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedByUserId = userId,
                        ReviewedByUserId = userId,
                        ReviewedAt = DateTime.UtcNow,
                        AdminComment = adminComment,
                        RejectionReason = string.Empty,
                        Status = ChangeLogStatus.Approved
                    };

                    _context.TitleChangeLogs.Add(changeLog);
                }

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
                        OldValue = oldValue,
                        NewValue = newValue,
                        AdminComment = adminComment,
                        IsAutoApproved = true
                    };

                    _context.ApprovedTitleChanges.Add(approvedChange);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create change log entry - tables may not exist");
                // Don't throw - this is optional functionality
            }
        }

        // Request DTOs
        public class RejectTitleRequest
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

    // Note: TitleApiController already exists in your project at FallenFaction.Server.Controllers.TitleApiController
    // This AdminTitleController handles admin-specific operations only
}