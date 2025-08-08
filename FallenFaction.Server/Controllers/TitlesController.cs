// Controllers/TitlesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.DTOs.Title;
using FallenFaction.Server.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using FallenFaction.Server.DTOs.Chapter;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TitlesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TitlesController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly Random _random;

        public TitlesController(
            ApplicationDbContext context,
            ILogger<TitlesController> logger,
            UserManager<AppUser> userManager,
            IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _hostingEnvironment = hostingEnvironment;
            _random = new Random();
        }

        #region Chapter Management Methods

        /// <summary>
        /// Get chapter creation form data for a specific title
        /// GET: api/Titles/{titleId}/chapters/create
        /// </summary>
        [HttpGet("{titleId:int}/chapters/create")]
        [Authorize]
        public async Task<ActionResult> GetChapterCreateForm(int titleId)
        {
            try
            {
                var title = await _context.Titles
                    .Include(t => t.Teams)
                    .Include(t => t.Chapters)
                    .FirstOrDefaultAsync(t => t.Id == titleId && t.IsAvailable);

                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Get teams the user is part of that are associated with this title
                var userTeams = await _context.Teams
                    .Where(t => t.Members.Contains(user) && title.Teams.Contains(t))
                    .Select(t => new { Id = t.Id, Name = t.Name })
                    .ToListAsync();

                if (!userTeams.Any())
                {
                    return StatusCode(403, new { message = "You do not have permission to add chapters to this title." });
                }

                // Get suggested next chapter/volume numbers
                var lastChapter = title.Chapters
                    .OrderByDescending(c => c.VolumeNumber)
                    .ThenByDescending(c => c.ChapterNumber)
                    .FirstOrDefault();

                var result = new
                {
                    TitleId = titleId,
                    TitleName = title.OriginalTitle,
                    UserTeams = userTeams,
                    SuggestedVolumeNumber = lastChapter?.VolumeNumber ?? 1,
                    SuggestedChapterNumber = (lastChapter?.ChapterNumber ?? 0) + 1
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapter create form data for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error retrieving chapter form data" });
            }
        }

        /// <summary>
        /// Create a new pending chapter
        /// POST: api/Titles/{titleId}/chapters
        /// </summary>
        [HttpPost("{titleId:int}/chapters")]
        [Authorize]
        public async Task<ActionResult> CreateChapter(int titleId, [FromForm] CreateChapterRequest request, [FromForm] List<IFormFile> chapterImages)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var title = await _context.Titles
                    .Include(t => t.Teams)
                    .FirstOrDefaultAsync(t => t.Id == titleId && t.IsAvailable);

                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                // Verify user has permission to add chapters to this title
                var userTeams = await _context.Teams
                    .Where(t => t.Members.Contains(user))
                    .ToListAsync();

                var team = userTeams.FirstOrDefault(t => t.Id == request.TeamId && title.Teams.Contains(t));
                if (team == null)
                {
                    return Forbid("You do not have permission to add chapters for this team/title combination.");
                }

                // Validate chapter images
                if (chapterImages == null || !chapterImages.Any())
                {
                    return BadRequest(new { message = "At least one chapter image is required" });
                }

                if (request.ImageOrders?.Length != chapterImages.Count)
                {
                    return BadRequest(new { message = "Each image must have an order number" });
                }

                // Create pending chapter
                var pendingChapter = new PendingChapter
                {
                    Name = request.Name,
                    VolumeNumber = request.VolumeNumber,
                    ChapterNumber = request.ChapterNumber,
                    TitleId = titleId,
                    TeamId = request.TeamId,
                    UpdatedByUserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow
                };

                _context.PendingChapters.Add(pendingChapter);
                await _context.SaveChangesAsync();

                // Handle image uploads
                var savedImages = await SaveChapterImages(chapterImages, request.ImageOrders, pendingChapter.Id, isPending: true);

                // Update pending chapter with images
                pendingChapter.ImagePaths = savedImages;
                _context.PendingChapters.Update(pendingChapter);
                await _context.SaveChangesAsync();

                var result = new
                {
                    Id = pendingChapter.Id,
                    Name = pendingChapter.Name,
                    VolumeNumber = pendingChapter.VolumeNumber,
                    ChapterNumber = pendingChapter.ChapterNumber,
                    TitleName = title.OriginalTitle,
                    TeamName = team.Name,
                    CreatedDate = pendingChapter.CreatedDate,
                    ImageCount = savedImages.Count
                };

                return CreatedAtAction(nameof(GetPendingChapter), new { id = pendingChapter.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error creating chapter", error = ex.Message });
            }
        }

        /// <summary>
        /// Get pending chapters for admin review
        /// GET: api/Titles/chapters/pending
        /// </summary>
        [HttpGet("chapters/pending")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> GetPendingChapters()
        {
            try
            {
                var pendingChapters = await _context.PendingChapters
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .Include(c => c.UpdatedByUser)
                    .Include(c => c.ImagePaths)
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Name = c.Name,
                        VolumeNumber = c.VolumeNumber,
                        ChapterNumber = c.ChapterNumber,
                        TitleName = c.Title.OriginalTitle,
                        TeamName = c.Team.Name,
                        CreatedDate = c.CreatedDate,
                        UpdatedByUserName = c.UpdatedByUser.UserName,
                        ImageCount = c.ImagePaths.Count
                    })
                    .ToListAsync();

                return Ok(pendingChapters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending chapters");
                return StatusCode(500, new { message = "Error fetching pending chapters" });
            }
        }

        /// <summary>
        /// Get specific pending chapter details
        /// GET: api/Titles/chapters/pending/{id}
        /// </summary>
        [HttpGet("chapters/pending/{id:int}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> GetPendingChapter(int id)
        {
            try
            {
                var pendingChapter = await _context.PendingChapters
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .Include(c => c.UpdatedByUser)
                    .Include(c => c.ImagePaths)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (pendingChapter == null)
                {
                    return NotFound(new { message = "Pending chapter not found" });
                }

                var result = new
                {
                    Id = pendingChapter.Id,
                    Name = pendingChapter.Name,
                    VolumeNumber = pendingChapter.VolumeNumber,
                    ChapterNumber = pendingChapter.ChapterNumber,
                    TitleId = pendingChapter.TitleId,
                    TitleName = pendingChapter.Title.OriginalTitle,
                    TeamId = pendingChapter.TeamId,
                    TeamName = pendingChapter.Team.Name,
                    CreatedDate = pendingChapter.CreatedDate,
                    UpdatedByUserId = pendingChapter.UpdatedByUserId,
                    UpdatedByUserName = pendingChapter.UpdatedByUser.UserName,
                    Images = pendingChapter.ImagePaths
                        .OrderBy(i => i.OrderIndex)
                        .Select(i => new ChapterImageDTO
                        {
                            Id = i.Id,
                            ImagePath = i.ImagePath,
                            OrderIndex = i.OrderIndex,
                            ChapterId = i.ChapterId
                        })
                        .ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending chapter {Id}", id);
                return StatusCode(500, new { message = "Error fetching pending chapter" });
            }
        }

        /// <summary>
        /// Approve a pending chapter
        /// POST: api/Titles/chapters/pending/{id}/approve
        /// </summary>
        [HttpPost("chapters/pending/{id:int}/approve")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<ChapterDTO>> ApproveChapter(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var pendingChapter = await _context.PendingChapters
                    .Include(pc => pc.ImagePaths)
                    .Include(pc => pc.Title)
                    .Include(pc => pc.Team)
                    .FirstOrDefaultAsync(pc => pc.Id == id);

                if (pendingChapter == null)
                {
                    return NotFound(new { message = "Pending chapter not found" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create the approved chapter
                    var chapter = new Chapter
                    {
                        Name = pendingChapter.Name,
                        VolumeNumber = pendingChapter.VolumeNumber,
                        ChapterNumber = pendingChapter.ChapterNumber,
                        TitleId = pendingChapter.TitleId,
                        TeamId = pendingChapter.TeamId,
                        CreatedDate = DateTime.UtcNow,
                        ReleaseDate = DateTime.UtcNow,
                        UpdatedByUserId = user.Id,
                        ImagePaths = new List<ChapterImage>()
                    };

                    _context.Chapters.Add(chapter);
                    await _context.SaveChangesAsync(); // Save to get the ID

                    // Transfer images from pending to approved chapter
                    foreach (var image in pendingChapter.ImagePaths)
                    {
                        image.PendingChapterId = null;
                        image.ChapterId = chapter.Id;
                        chapter.ImagePaths.Add(image);
                    }

                    // Remove the pending chapter
                    _context.PendingChapters.Remove(pendingChapter);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Load the chapter with all relationships for DTO mapping
                    var fullChapter = await _context.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .Include(c => c.ImagePaths)
                        .FirstOrDefaultAsync(c => c.Id == chapter.Id);

                    var result = ChapterMapper.ToDTO(fullChapter);

                    return Ok(result);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving chapter {Id}", id);
                return StatusCode(500, new { message = "Error approving chapter" });
            }
        }

        /// <summary>
        /// Reject a pending chapter
        /// POST: api/Titles/chapters/pending/{id}/reject
        /// </summary>
        [HttpPost("chapters/pending/{id:int}/reject")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> RejectChapter(int id, [FromBody] RejectChapterRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var pendingChapter = await _context.PendingChapters
                    .Include(pc => pc.ImagePaths)
                    .FirstOrDefaultAsync(pc => pc.Id == id);

                if (pendingChapter == null)
                {
                    return NotFound(new { message = "Pending chapter not found" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create rejected chapter record
                    var rejectedChapter = new RejectedChapter
                    {
                        Name = pendingChapter.Name,
                        VolumeNumber = pendingChapter.VolumeNumber,
                        ChapterNumber = pendingChapter.ChapterNumber,
                        TitleId = pendingChapter.TitleId,
                        TeamId = pendingChapter.TeamId,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedByUserId = user.Id,
                        ImagePaths = new List<ChapterImage>()
                    };

                    _context.RejectedChapters.Add(rejectedChapter);
                    await _context.SaveChangesAsync(); // Save to get the ID

                    // Transfer images from pending to rejected chapter
                    foreach (var image in pendingChapter.ImagePaths)
                    {
                        image.PendingChapterId = null;
                        image.RejectedChapterId = rejectedChapter.Id;
                        rejectedChapter.ImagePaths.Add(image);
                    }

                    // Remove the pending chapter
                    _context.PendingChapters.Remove(pendingChapter);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new { message = "Chapter rejected successfully", reason = request?.Reason });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting chapter {Id}", id);
                return StatusCode(500, new { message = "Error rejecting chapter" });
            }
        }

        /// <summary>
        /// Get chapter details for reading
        /// GET: api/Titles/{titleId}/chapters/{chapterNumber}
        /// </summary>
        [HttpGet("{titleId:int}/chapters/{chapterNumber:int}")]
        public async Task<ActionResult<ChapterDTO>> GetChapterForReading(
            int titleId,
            int chapterNumber,
            [FromQuery] int? volumeNumber = null,
            [FromQuery] int? teamId = null,
            [FromQuery] int? page = null)
        {
            try
            {
                var query = _context.Chapters
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .Include(c => c.ImagePaths)
                    .Where(c => c.TitleId == titleId && c.ChapterNumber == chapterNumber);

                if (volumeNumber.HasValue)
                {
                    query = query.Where(c => c.VolumeNumber == volumeNumber.Value);
                }

                if (teamId.HasValue)
                {
                    query = query.Where(c => c.TeamId == teamId.Value);
                }

                var chapter = await query.FirstOrDefaultAsync();

                if (chapter == null)
                {
                    return NotFound(new { message = "Chapter not found" });
                }

                if (!chapter.Title.IsAvailable)
                {
                    return NotFound(new { message = "Title is not available" });
                }

                // Convert to DTO using existing mapper
                var chapterDto = ChapterMapper.ToDTO(chapter);

                // Add navigation info (previous/next chapters) - this matches the old controller logic
                await EnrichWithAdjacentChapters(chapterDto);

                // Log chapter view (if user is authenticated)
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await LogChapterView(chapter.Id, user.Id);
                }

                return Ok(chapterDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapter {ChapterNumber} for title {TitleId}", chapterNumber, titleId);
                return StatusCode(500, new { message = "Error retrieving chapter" });
            }
        }

        /// <summary>
        /// Get chapters list for a title
        /// GET: api/Titles/{titleId}/chapters
        /// </summary>
        [HttpGet("{titleId:int}/chapters")]
        public async Task<ActionResult<IEnumerable<ChapterDTO>>> GetChaptersForTitle(int titleId)
        {
            try
            {
                var title = await _context.Titles.FirstOrDefaultAsync(t => t.Id == titleId && t.IsAvailable);
                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                var chapters = await _context.Chapters
                    .Include(c => c.Team)
                    .Include(c => c.Title)
                    .Include(c => c.ImagePaths)
                    .Where(c => c.TitleId == titleId)
                    .OrderByDescending(c => c.VolumeNumber)
                    .ThenByDescending(c => c.ChapterNumber)
                    .ToListAsync();

                var chapterDtos = chapters.Select(ChapterMapper.ToDTO).ToList();

                return Ok(chapterDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error retrieving chapters" });
            }
        }

        /// <summary>
        /// Get chapter by its route parameters (matches old controller route pattern)
        /// GET: api/Titles/{titleName}/chapter/{chapterName}/v{volume}/t{teamId}
        /// </summary>
        [HttpGet("{titleName}/chapter/{chapterName}/v{volume:int}/t{teamId:int}")]
        public async Task<ActionResult<ChapterDTO>> GetChapterByRoute(string titleName, string chapterName, int volume, int teamId, [FromQuery] int? page = null)
        {
            try
            {
                // Decode the URL-encoded title name
                string decodedTitleName = Uri.UnescapeDataString(titleName);

                var chapter = await _context.Chapters
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .Include(c => c.ImagePaths)
                    .FirstOrDefaultAsync(c => c.Title.OriginalTitle == decodedTitleName &&
                                            c.Name == chapterName &&
                                            c.VolumeNumber == volume &&
                                            c.TeamId == teamId);

                if (chapter == null)
                {
                    return NotFound(new { message = "Chapter not found" });
                }

                if (!chapter.Title.IsAvailable)
                {
                    return NotFound(new { message = "Title is not available" });
                }

                // Convert to DTO
                var chapterDto = ChapterMapper.ToDTO(chapter);

                // Add information about next and previous chapters
                await EnrichWithAdjacentChapters(chapterDto);

                // Log chapter view
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await LogChapterView(chapter.Id, user.Id);
                }

                return Ok(chapterDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapter by route");
                return StatusCode(500, new { message = "Error retrieving chapter" });
            }
        }

        #endregion

        #region Helper Methods

        private async Task<List<ChapterImage>> SaveChapterImages(List<IFormFile> images, int[] orders, int chapterId, bool isPending = false)
        {
            var uploads = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "chapters");
            Directory.CreateDirectory(uploads);

            var savedImages = new List<ChapterImage>();

            for (int i = 0; i < images.Count; i++)
            {
                var image = images[i];
                var order = orders[i];

                if (image.Length > 0)
                {
                    var extension = Path.GetExtension(image.FileName);
                    var fileName = $"{chapterId}-{order}{extension}";
                    var filePath = Path.Combine(uploads, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    var chapterImage = new ChapterImage
                    {
                        ImagePath = $"/uploads/chapters/{fileName}",
                        OrderIndex = order,
                        PendingChapterId = isPending ? chapterId : null,
                        ChapterId = isPending ? null : chapterId
                    };

                    savedImages.Add(chapterImage);
                }
            }

            return savedImages;
        }

        // Helper method to add adjacent chapter information (from old controller)
        private async Task EnrichWithAdjacentChapters(ChapterDTO chapterDto)
        {
            // Get title information to find adjacent chapters
            var title = await _context.Titles
                .Include(t => t.Chapters)
                .ThenInclude(c => c.ImagePaths)  // Include image paths to get page count
                .FirstOrDefaultAsync(t => t.Id == chapterDto.TitleId);

            if (title == null) return;

            // Get ordered list of chapters for this title
            var orderedChapters = title.Chapters
                .OrderBy(c => c.VolumeNumber)
                .ThenBy(c => c.ChapterNumber)
                .ToList();

            // Find current chapter's index
            int currentIndex = orderedChapters.FindIndex(c => c.Id == chapterDto.Id);
            if (currentIndex == -1) return;

            // Get next chapter
            if (currentIndex < orderedChapters.Count - 1)
            {
                var nextChapter = orderedChapters[currentIndex + 1];
                chapterDto.NextChapterId = nextChapter.Id;
                chapterDto.NextChapterName = nextChapter.Name;
                chapterDto.NextChapterVolume = nextChapter.VolumeNumber;
                chapterDto.NextChapterTeamId = nextChapter.TeamId;
            }

            // Get previous chapter
            if (currentIndex > 0)
            {
                var prevChapter = orderedChapters[currentIndex - 1];
                chapterDto.PreviousChapterId = prevChapter.Id;
                chapterDto.PreviousChapterName = prevChapter.Name;
                chapterDto.PreviousChapterVolume = prevChapter.VolumeNumber;
                chapterDto.PreviousChapterTeamId = prevChapter.TeamId;

                // Get the page count of the previous chapter
                chapterDto.PreviousChapterPageCount = prevChapter.ImagePaths?.Count ?? 0;
            }
        }

        private async Task LogChapterView(int chapterId, string userId)
        {
            try
            {
                // Check if user already viewed this chapter recently (within last hour)
                var recentView = await _context.ChapterViews
                    .Where(cv => cv.ChapterId == chapterId &&
                                cv.UserId == userId &&
                                cv.ViewDate >= DateTime.UtcNow.AddHours(-1))
                    .FirstOrDefaultAsync();

                if (recentView == null)
                {
                    var chapterView = new ChapterView
                    {
                        ChapterId = chapterId,
                        UserId = userId,
                        ViewDate = DateTime.UtcNow,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                    };

                    _context.ChapterViews.Add(chapterView);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error logging chapter view for chapter {ChapterId}", chapterId);
                // Don't throw - this is non-critical functionality
            }
        }

        #endregion

        #region Request Models

        public class CreateChapterRequest
        {
            public string Name { get; set; } = string.Empty;
            public int VolumeNumber { get; set; }
            public int ChapterNumber { get; set; }
            public int TeamId { get; set; }
            public int[]? ImageOrders { get; set; }
        }

        public class RejectChapterRequest
        {
            public string? Reason { get; set; }
        }

        #endregion

        // ... [Rest of existing methods remain the same] ...

        /// <summary>
        /// Get debug info about database state
        /// GET: api/Titles/Debug
        /// </summary>
        [HttpGet("Debug")]
        public async Task<ActionResult> GetDebugInfo()
        {
            try
            {
                var titleCount = await _context.Titles.CountAsync();
                var availableTitleCount = await _context.Titles.CountAsync(t => t.IsAvailable);
                var userCount = await _context.Users.CountAsync();
                var teamCount = await _context.Teams.CountAsync();
                var chapterCount = await _context.Chapters.CountAsync();
                var pendingChapterCount = await _context.PendingChapters.CountAsync();
                var chapterViewCount = await _context.ChapterViews.CountAsync();
                var ratingCount = await _context.Ratings.CountAsync();
                var bookmarkCount = await _context.Bookmarks.CountAsync();
                var bookmarkFolderCount = await _context.BookmarkFolders.CountAsync();

                var sampleTitles = await _context.Titles
                    .Include(t => t.Chapters)
                    .Include(t => t.Ratings)
                    .Include(t => t.Bookmarks)
                    .Take(3)
                    .Select(t => new {
                        t.Id,
                        t.OriginalTitle,
                        t.EnglishTitle,
                        t.IsAvailable,
                        t.ReleaseDate,
                        ChapterCount = t.Chapters.Count(),
                        LatestChapter = t.Chapters.Any() ? t.Chapters.Max(c => c.ChapterNumber) : 0,
                        LastUpdate = t.Chapters.Any() ? t.Chapters.Max(c => c.ReleaseDate) : (DateTime?)null,
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => (double)r.Value) : 0.0,
                        BookmarkCount = t.Bookmarks.Count(),
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalTitles = titleCount,
                    AvailableTitles = availableTitleCount,
                    TotalUsers = userCount,
                    TotalTeams = teamCount,
                    TotalChapters = chapterCount,
                    PendingChapters = pendingChapterCount,
                    TotalChapterViews = chapterViewCount,
                    TotalRatings = ratingCount,
                    TotalBookmarks = bookmarkCount,
                    TotalBookmarkFolders = bookmarkFolderCount,
                    SampleTitles = sampleTitles,
                    DatabaseConnected = true,
                    ModelInfo = new
                    {
                        RatingScale = "1-10 (integer)",
                        BookmarkSystem = "Folder-based with LastReadChapter tracking",
                        ChapterViewTracking = "Per-user view tracking with IP and UserAgent",
                        CommentSystem = "Nested comments with reactions"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debug info");
                return Ok(new
                {
                    Error = ex.Message,
                    DatabaseConnected = false
                });
            }
        }

        /// <summary>
        /// Get featured titles for homepage
        /// GET: api/Titles/Featured
        /// </summary>
        [HttpGet("Featured")]
        public async Task<ActionResult<IEnumerable<TitleFeaturedDto>>> GetFeaturedTitles()
        {
            try
            {
                _logger.LogInformation("Fetching featured titles");

                var featuredTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .Include(t => t.Chapters)
                    .Select(t => new TitleFeaturedDto
                    {
                        Id = t.Id,
                        OriginalTitle = t.OriginalTitle ?? "Unknown Title",
                        EnglishTitle = t.EnglishTitle ?? t.OriginalTitle ?? "Unknown Title",
                        CoverImagePath = !string.IsNullOrEmpty(t.CoverImagePath) ? t.CoverImagePath : "/img/logo.png",
                        Type = t.Type,
                        LatestChapter = t.Chapters.Any() ?
                            t.Chapters.Max(c => c.ChapterNumber).ToString() :
                            "No chapters",
                        Description = t.Description ?? "",
                        ReleaseDate = t.ReleaseDate ?? "Unknown",
                        ChapterCount = t.Chapters.Count(),
                        LastUpdated = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            (DateTime?)null
                    })
                    .OrderBy(x => Guid.NewGuid()) // Better randomization
                    .Take(14)
                    .ToListAsync();

                _logger.LogInformation($"Returning {featuredTitles.Count} featured titles");
                return Ok(featuredTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching featured titles", error = ex.Message });
            }
        }

        /// <summary>
        /// Get popular titles for homepage (ordered by chapter count and ratings)
        /// GET: api/Titles/Popular
        /// </summary>
        [HttpGet("Popular")]
        public async Task<ActionResult<IEnumerable<TitleListDto>>> GetPopularTitles()
        {
            try
            {
                _logger.LogInformation("Fetching popular titles");

                var popularTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .Include(t => t.Chapters)
                    .ThenInclude(c => c.Views)
                    .Include(t => t.Ratings)
                    .Include(t => t.Bookmarks)
                    .Select(t => new
                    {
                        Title = t,
                        ChapterCount = t.Chapters.Count(),
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count(),
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => r.Value) : 0,
                        BookmarkCount = t.Bookmarks.Count(),
                        LastChapterDate = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            DateTime.MinValue,
                        PopularityScore = (t.Chapters.Count() * 2) +
                                       (t.Chapters.SelectMany(c => c.Views).Count() * 0.1) +
                                       (t.Ratings.Any() ? t.Ratings.Average(r => r.Value) * 10 : 0) +
                                       (t.Bookmarks.Count() * 5)
                    })
                    .OrderByDescending(x => x.PopularityScore)
                    .Take(20)
                    .ToListAsync();

                var result = popularTitles.Select(item => new TitleListDto
                {
                    Id = item.Title.Id,
                    OriginalTitle = item.Title.OriginalTitle ?? "Unknown Title",
                    EnglishTitle = item.Title.EnglishTitle ?? item.Title.OriginalTitle ?? "Unknown Title",
                    CoverImagePath = !string.IsNullOrEmpty(item.Title.CoverImagePath) ? item.Title.CoverImagePath : "/img/logo.png",
                    Type = item.Title.Type,
                    LatestChapter = item.ChapterCount > 0 ?
                        item.ChapterCount.ToString() :
                        "No chapters",
                    LastUpdated = item.LastChapterDate != DateTime.MinValue ? item.LastChapterDate : null,
                    ChapterCount = item.ChapterCount,
                    AverageRating = item.AverageRating,
                    BookmarkCount = item.BookmarkCount,
                    ReleaseDate = item.Title.ReleaseDate ?? "Unknown"
                }).ToList();

                _logger.LogInformation($"Returning {result.Count} popular titles");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching popular titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching popular titles", error = ex.Message });
            }
        }

        /// <summary>
        /// Get recent updates for homepage (ordered by actual update time)
        /// GET: api/Titles/RecentUpdates
        /// </summary>
        [HttpGet("RecentUpdates")]
        public async Task<ActionResult<IEnumerable<TitleUpdateDto>>> GetRecentUpdates()
        {
            try
            {
                _logger.LogInformation("Fetching recent updates");

                var recentUpdates = await _context.Titles
                    .Where(t => t.IsAvailable && t.Chapters.Any())
                    .Include(t => t.Teams)
                    .Include(t => t.Chapters)
                        .ThenInclude(c => c.Team)
                    .Select(t => new
                    {
                        Title = t,
                        LastChapterDate = t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate,
                        LatestChapterNumber = t.Chapters.Max(c => c.ChapterNumber),
                        LatestChapterName = t.Chapters.OrderByDescending(c => c.ReleaseDate).First().Name,
                        LatestChapterTeam = t.Chapters.OrderByDescending(c => c.ReleaseDate).First().Team,
                        LatestChapterVolume = t.Chapters.OrderByDescending(c => c.ReleaseDate).First().VolumeNumber
                    })
                    .OrderByDescending(x => x.LastChapterDate)
                    .Take(10)
                    .ToListAsync();

                var result = recentUpdates.Select(item => new TitleUpdateDto
                {
                    Id = item.Title.Id,
                    OriginalTitle = item.Title.OriginalTitle ?? "Unknown Title",
                    CoverImagePath = !string.IsNullOrEmpty(item.Title.CoverImagePath) ? item.Title.CoverImagePath : "/img/logo.png",
                    Description = !string.IsNullOrEmpty(item.Title.Description) && item.Title.Description.Length > 200
                        ? item.Title.Description.Substring(0, 200) + "..."
                        : item.Title.Description ?? "No description available",
                    TeamName = item.LatestChapterTeam?.Name ??
                              (item.Title.Teams.Any() ? item.Title.Teams.First().Name : "Unknown Team"),
                    TimeAgo = GetTimeAgo(item.LastChapterDate),
                    LatestChapter = $"Vol.{item.LatestChapterVolume} Ch.{item.LatestChapterNumber}" +
                                   (!string.IsNullOrEmpty(item.LatestChapterName) ? $": {item.LatestChapterName}" : ""),
                    LastUpdated = item.LastChapterDate
                }).ToList();

                _logger.LogInformation($"Returning {result.Count} recent updates");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent updates: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching recent updates", error = ex.Message });
            }
        }

        /// <summary>
        /// Get title details by encoded title name (for URL routing)
        /// GET: api/Titles/Details/{encodedTitle}
        /// </summary>
        [HttpGet("Details/{encodedTitle}")]
        public async Task<ActionResult<TitleDetailDto>> GetTitleByName(string encodedTitle)
        {
            try
            {
                var decodedTitle = Uri.UnescapeDataString(encodedTitle);
                _logger.LogInformation($"Looking for title: {decodedTitle}");

                var titleData = await _context.Titles
                    .Where(t => t.IsAvailable && t.OriginalTitle == decodedTitle)
                    .Include(t => t.Chapters)
                        .ThenInclude(c => c.Views)
                    .Include(t => t.Teams)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Ratings)
                    .Include(t => t.Bookmarks)
                        .ThenInclude(b => b.Folder)
                    .Select(t => new
                    {
                        Title = t,
                        ChapterCount = t.Chapters.Count(),
                        LatestChapterNumber = t.Chapters.Any() ? t.Chapters.Max(c => c.ChapterNumber) : 0,
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => (double)r.Value) : 0.0,
                        RatingCount = t.Ratings.Count(),
                        BookmarkCount = t.Bookmarks.Count(),
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count(),
                        LastUpdated = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            (DateTime?)null
                    })
                    .FirstOrDefaultAsync();

                if (titleData == null)
                {
                    _logger.LogWarning($"Title not found: {decodedTitle}");
                    return NotFound(new { message = "Title not found" });
                }

                var titleDto = new TitleDetailDto
                {
                    Id = titleData.Title.Id,
                    OriginalTitle = titleData.Title.OriginalTitle,
                    EnglishTitle = titleData.Title.EnglishTitle ?? titleData.Title.OriginalTitle,
                    Description = titleData.Title.Description ?? "",
                    CoverImagePath = !string.IsNullOrEmpty(titleData.Title.CoverImagePath) ? titleData.Title.CoverImagePath : "/img/logo.png",
                    BackgroundImagePath = titleData.Title.BackgroundImagePath,
                    Type = titleData.Title.Type,
                    StatusTitle = titleData.Title.StatusTitle ?? "Unknown",
                    StatusTranslation = titleData.Title.StatusTranslation ?? "Unknown",
                    ReleaseDate = titleData.Title.ReleaseDate ?? "Unknown",
                    AgeRestriction = titleData.Title.AgeRestriction,
                    ChapterCount = titleData.ChapterCount,
                    LatestChapter = titleData.LatestChapterNumber > 0 ? titleData.LatestChapterNumber.ToString() : "No chapters",
                    AverageRating = titleData.AverageRating,
                    RatingCount = titleData.RatingCount,
                    BookmarkCount = titleData.BookmarkCount,
                    ViewCount = titleData.ViewCount,
                    LastUpdated = titleData.LastUpdated,
                    Teams = titleData.Title.Teams.Select(team => team.Name).ToList(),
                    Authors = titleData.Title.Authors.Select(author => author.Name).ToList(),
                    Artists = titleData.Title.Artists.Select(artist => artist.Name).ToList(),
                    Categories = titleData.Title.Categories.Select(cat => cat.Name).ToList(),
                    Tags = titleData.Title.Tags.Select(tag => tag.Name).ToList()
                };

                return Ok(titleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching title details for {EncodedTitle}: {Error}", encodedTitle, ex.Message);
                return StatusCode(500, new { message = "Error fetching title details", error = ex.Message });
            }
        }

        // Helper methods
        private async Task<int> GetLatestChapterNumberForTitle(int titleId)
        {
            try
            {
                var latestChapter = await _context.Chapters
                    .Where(c => c.TitleId == titleId)
                    .OrderByDescending(c => c.ChapterNumber)
                    .FirstOrDefaultAsync();

                return latestChapter?.ChapterNumber ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting latest chapter for title {TitleId}", titleId);
                return 0;
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} min ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) > 1 ? "s" : "")} ago";

            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) > 1 ? "s" : "")} ago";
        }

        /// <summary>
        /// Get user's bookmarked titles with reading progress
        /// GET: api/Titles/UserBookmarks
        /// </summary>
        [HttpGet("UserBookmarks")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserBookmarks()
        {
            try
            {
                var userId = User?.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var userBookmarks = await _context.Bookmarks
                    .Where(b => b.UserId == userId)
                    .Include(b => b.Title)
                        .ThenInclude(t => t.Chapters)
                    .Include(b => b.Folder)
                    .Select(b => new
                    {
                        BookmarkId = b.Id,
                        TitleId = b.TitleId,
                        Title = b.Title.OriginalTitle,
                        EnglishTitle = b.Title.EnglishTitle,
                        CoverImage = b.Title.CoverImagePath,
                        Type = b.Title.Type,
                        LastReadChapter = b.LastReadChapter,
                        TotalChapters = b.Title.Chapters.Count(),
                        LatestChapter = b.Title.Chapters.Any() ? b.Title.Chapters.Max(c => c.ChapterNumber) : 0,
                        FolderName = b.Folder.Name,
                        AddedDate = b.AddedDate,
                        ReadingProgress = b.Title.Chapters.Any() ?
                            (double)b.LastReadChapter / b.Title.Chapters.Max(c => c.ChapterNumber) * 100 : 0,
                        LastUpdate = b.Title.Chapters.Any() ?
                            b.Title.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            (DateTime?)null
                    })
                    .OrderByDescending(b => b.AddedDate)
                    .ToListAsync();

                return Ok(userBookmarks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user bookmarks");
                return StatusCode(500, new { message = "Error fetching bookmarks", error = ex.Message });
            }
        }

        /// <summary>
        /// Get trending titles based on recent views and activity
        /// GET: api/Titles/Trending
        /// </summary>
        [HttpGet("Trending")]
        public async Task<ActionResult<IEnumerable<TitleListDto>>> GetTrendingTitles()
        {
            try
            {
                _logger.LogInformation("Fetching trending titles");

                var trendingTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .Include(t => t.Chapters)
                        .ThenInclude(c => c.Views)
                    .Include(t => t.Ratings)
                    .Include(t => t.Bookmarks)
                    .Select(t => new
                    {
                        Title = t,
                        ChapterCount = t.Chapters.Count(),
                        RecentViews = t.Chapters
                            .SelectMany(c => c.Views)
                            .Count(v => v.ViewDate >= DateTime.UtcNow.AddDays(-7)), // Views in last 7 days
                        RecentRatings = t.Ratings
                            .Count(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-30)), // Ratings in last 30 days
                        RecentBookmarks = t.Bookmarks
                            .Count(b => b.AddedDate >= DateTime.UtcNow.AddDays(-30)), // Bookmarks in last 30 days
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => (double)r.Value) : 0.0,
                        LastChapterDate = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            DateTime.MinValue,
                        TrendingScore =
                            (t.Chapters.SelectMany(c => c.Views).Count(v => v.ViewDate >= DateTime.UtcNow.AddDays(-7)) * 1.0) +
                            (t.Ratings.Count(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-30)) * 5.0) +
                            (t.Bookmarks.Count(b => b.AddedDate >= DateTime.UtcNow.AddDays(-30)) * 10.0) +
                            (t.Chapters.Any() && t.Chapters.Max(c => c.ReleaseDate) >= DateTime.UtcNow.AddDays(-7) ? 20.0 : 0)
                    })
                    .Where(x => x.TrendingScore > 0)
                    .OrderByDescending(x => x.TrendingScore)
                    .Take(15)
                    .ToListAsync();

                var result = trendingTitles.Select(item => new TitleListDto
                {
                    Id = item.Title.Id,
                    OriginalTitle = item.Title.OriginalTitle ?? "Unknown Title",
                    EnglishTitle = item.Title.EnglishTitle ?? item.Title.OriginalTitle ?? "Unknown Title",
                    CoverImagePath = !string.IsNullOrEmpty(item.Title.CoverImagePath) ? item.Title.CoverImagePath : "/img/logo.png",
                    Type = item.Title.Type,
                    LatestChapter = item.ChapterCount > 0 ? item.ChapterCount.ToString() : "No chapters",
                    LastUpdated = item.LastChapterDate != DateTime.MinValue ? item.LastChapterDate : null,
                    ChapterCount = item.ChapterCount,
                    AverageRating = item.AverageRating,
                    BookmarkCount = item.RecentBookmarks, // Use recent bookmarks for trending
                    ReleaseDate = item.Title.ReleaseDate ?? "Unknown"
                }).ToList();

                _logger.LogInformation($"Returning {result.Count} trending titles");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trending titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching trending titles", error = ex.Message });
            }
        }



        /// <summary>
        /// Get chapter list for a title (for chapter navigation popup)
        /// GET: api/Titles/{titleId}/chapters/list
        /// </summary>
        [HttpGet("{titleId:int}/chapters/list")]
        public async Task<ActionResult<IEnumerable<object>>> GetChaptersList(int titleId)
        {
            try
            {
                var title = await _context.Titles.FirstOrDefaultAsync(t => t.Id == titleId && t.IsAvailable);
                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                var chapters = await _context.Chapters
                    .Include(c => c.Team)
                    .Where(c => c.TitleId == titleId)
                    .OrderByDescending(c => c.VolumeNumber)
                    .ThenByDescending(c => c.ChapterNumber)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.VolumeNumber,
                        c.ChapterNumber,
                        c.TeamId,
                        TeamName = c.Team.Name,
                        c.ReleaseDate
                    })
                    .ToListAsync();

                return Ok(chapters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters list for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error retrieving chapters list" });
            }
        }

        /// <summary>
        /// Update reading progress for authenticated user
        /// POST: api/Titles/updateProgress
        /// </summary>
        [HttpPost("updateProgress")]
        [Authorize]
        public async Task<ActionResult> UpdateReadingProgress([FromBody] UpdateProgressRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Find or create bookmark
                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.UserId == user.Id && b.TitleId == request.TitleId);

                if (bookmark == null)
                {
                    // Create new bookmark in default folder
                    var defaultFolder = await _context.BookmarkFolders
                        .FirstOrDefaultAsync(f => f.UserId == user.Id && f.Name == "Reading");

                    if (defaultFolder == null)
                    {
                        // Create default folder if it doesn't exist
                        defaultFolder = new BookmarkFolder
                        {
                            UserId = user.Id,
                            Name = "Reading",
                            IsDefault = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.BookmarkFolders.Add(defaultFolder);
                        await _context.SaveChangesAsync();
                    }

                    bookmark = new Bookmark
                    {
                        UserId = user.Id,
                        TitleId = request.TitleId,
                        FolderId = defaultFolder.Id,
                        LastReadChapter = request.ChapterNumber,
                        AddedDate = DateTime.UtcNow,
                        LastReadDate = DateTime.UtcNow
                    };
                    _context.Bookmarks.Add(bookmark);
                }
                else
                {
                    // Update existing bookmark
                    bookmark.LastReadChapter = Math.Max(bookmark.LastReadChapter, request.ChapterNumber);
                    bookmark.LastReadDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Progress updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reading progress");
                return StatusCode(500, new { message = "Error updating progress" });
            }
        }

        public class UpdateProgressRequest
        {
            public int TitleId { get; set; }
            public int ChapterNumber { get; set; }
        }

    }
}