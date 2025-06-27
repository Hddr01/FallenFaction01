using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using System.ComponentModel.DataAnnotations;

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

        // GET: api/TitleApi/form-data
        [HttpGet("form-data")]
        public async Task<IActionResult> GetFormData()
        {
            try
            {
                var formData = new
                {
                    Authors = await _context.Set<Author>().Select(a => new { a.Id, a.Name }).ToListAsync(),
                    Artists = await _context.Set<Artist>().Select(a => new { a.Id, a.Name }).ToListAsync(),
                    Publishers = await _context.Set<Publisher>().Select(p => new { p.Id, p.Name }).ToListAsync(),
                    Teams = await _context.Set<Team>().Select(t => new { t.Id, t.Name }).ToListAsync(),
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

        // POST: api/TitleApi/create
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

                // For now, we'll make team optional since you mentioned only needing team and english title
                // but the frontend might not always provide a team

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
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

                _logger.LogInformation($"Pending title created successfully: {pendingTitle.EnglishTitle} by user {user.UserName}");

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

            // Update collections
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
                var teams = await _context.Set<Team>().Where(t => request.Teams.Contains(t.Id)).ToListAsync();
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

        // GET: api/TitleApi/pending
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")] // Only admins can view pending titles
        public async Task<IActionResult> GetPendingTitles()
        {
            try
            {
                var pendingTitles = await _context.Set<PendingTitle>()
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Select(t => new
                    {
                        t.Id,
                        t.EnglishTitle,
                        t.OriginalTitle,
                        t.Type,
                        t.CoverImagePath,
                        Authors = t.Authors.Select(a => a.Name).ToList(),
                        Teams = t.Teams.Select(team => team.Name).ToList(),
                        Categories = t.Categories.Select(c => c.Name).ToList()
                    })
                    .ToListAsync();

                return Ok(pendingTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending titles");
                return StatusCode(500, new { error = "Failed to load pending titles" });
            }
        }

        // POST: api/TitleApi/approve/{id}
        [HttpPost("approve/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePendingTitle(int id)
        {
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
    }

    // Request model for creating titles
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
}