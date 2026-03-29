// Controllers/TitlesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.DTOs.Title;
using FallenFaction.Server.DTOs.Team;
using FallenFaction.Server.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using FallenFaction.Server.DTOs.Chapter;
using FallenFaction.Server.Services.Interfaces;

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
        private readonly ITrustService _trustService;

        public TitlesController(
            ApplicationDbContext context,
            ILogger<TitlesController> logger,
            UserManager<AppUser> userManager,
            IWebHostEnvironment hostingEnvironment,
            ITrustService trustService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _hostingEnvironment = hostingEnvironment;
            _random = new Random();
            _trustService = trustService;
        }

        #region Chapter Management Methods

        /// <summary>
        /// Get chapter creation form data for a specific title - UPDATED with authorization
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

                // AUTHORIZATION CHECK: Get teams the user can add chapters to for this title
                var authorizedTeamIds = await GetAuthorizedTeamIds(user.Id, "CanAddChapter");
                var titleTeamIds = title.Teams.Select(t => t.Id).ToList();
                var validTeamIds = authorizedTeamIds.Intersect(titleTeamIds).ToList();

                if (!validTeamIds.Any())
                {
                    return StatusCode(403, new { message = "You do not have permission to add chapters to this title." });
                }

                // Get only the teams user can add chapters to
                var userTeams = await _context.Teams
                    .Where(t => validTeamIds.Contains(t.Id))
                    .Select(t => new { Id = t.Id, Name = t.Name })
                    .ToListAsync();

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
        /// Create a new pending chapter - UPDATED with authorization
        /// POST: api/Titles/{titleId}/chapters
        /// </summary>
        [HttpPost("{titleId:int}/chapters")]
        [Authorize]
        public async Task<ActionResult> CreateChapter(int titleId, [FromBody] CreateChapterRequest request)
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

                // AUTHORIZATION CHECK: Verify user can add chapters to the selected team for this title
                var authorizedTeamIds = await GetAuthorizedTeamIds(user.Id, "CanAddChapter");

                if (!authorizedTeamIds.Contains(request.TeamId))
                {
                    return Forbid("You do not have permission to add chapters for this team.");
                }

                // Verify the team is associated with this title
                if (!title.Teams.Any(t => t.Id == request.TeamId))
                {
                    return BadRequest(new { message = "Selected team is not associated with this title" });
                }

                // Validate chapter content
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { message = "Chapter content cannot be empty" });
                }

                // ── Trust auto-approve check ────────────────────────────────────────
                bool isTrustedForChapter = await _trustService.IsTrustedAsync(user.Id, TrustActionType.AddChapter);

                if (isTrustedForChapter)
                {
                    var autoChapter = new Chapter
                    {
                        Name = request.Name,
                        VolumeNumber = request.VolumeNumber,
                        ChapterNumber = request.ChapterNumber,
                        TitleId = titleId,
                        TeamId = request.TeamId,
                        UpdatedByUserId = user.Id,
                        CreatedDate = DateTime.UtcNow,
                        ReleaseDate = DateTime.UtcNow,
                        Content = request.Content
                    };
                    _context.Chapters.Add(autoChapter);
                    await _context.SaveChangesAsync();

                    // Write system change log
                    var autoLog = new TitleChangeLog
                    {
                        TitleId = titleId,
                        UpdatedByUserId = user.Id,
                        ReviewedByUserId = null,
                        CreatedAt = DateTime.UtcNow,
                        ReviewedAt = DateTime.UtcNow,
                        ChangeType = "Add Chapter",
                        OldValue = "",
                        NewValue = $"Ch.{request.ChapterNumber} - {request.Name}",
                        AdminComment = "Auto-approved by system (trusted user)",
                        Status = ChangeLogStatus.AutoApproved,
                    };
                    _context.TitleChangeLogs.Add(autoLog);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Chapter auto-approved for trusted user {User}: Ch.{Num}", user.UserName, request.ChapterNumber);
                    return Ok(new { message = "Chapter auto-approved and published!", chapterId = autoChapter.Id, autoApproved = true });
                }

                // ── Standard pending flow ────────────────────────────────────────────
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
                    CreatedDate = DateTime.UtcNow,
                    Content = request.Content
                };

                _context.PendingChapters.Add(pendingChapter);
                await _context.SaveChangesAsync();

                var team = await _context.Teams.FindAsync(request.TeamId);
                var result = new
                {
                    Id = pendingChapter.Id,
                    Name = pendingChapter.Name,
                    VolumeNumber = pendingChapter.VolumeNumber,
                    ChapterNumber = pendingChapter.ChapterNumber,
                    TitleName = title.OriginalTitle,
                    TeamName = team?.Name,
                    CreatedDate = pendingChapter.CreatedDate,
                    WordCount = request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                };

                _logger.LogInformation("Chapter created by user {UserName} for title {TitleName}, team {TeamName}",
                    user.UserName, title.OriginalTitle, team?.Name);

                return CreatedAtAction(nameof(GetPendingChapter), new { id = pendingChapter.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error creating chapter", error = ex.Message });
            }
        }
        // HELPER METHODS: Add these to TitlesController.cs

        /// <summary>
        /// Get teams user can perform specific action on
        /// </summary>
        // FOR TitleApiController.cs - Update the GetAuthorizedTeamIds method:
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
                    // Members with specific permission - UPDATED permission names
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == permission))
                )
                .Select(utr => utr.TeamId)
                .Distinct()
                .ToListAsync();

            return authorizedTeamIds;
        }

        /// <summary>
        /// Check if user can edit specific chapter
        /// </summary>
        private async Task<bool> CanUserEditChapter(string userId, Chapter chapter)
        {
            // Admins can edit all chapters
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    return true;
                }
            }

            // Check if user created the chapter
            if (chapter.UpdatedByUserId == userId)
            {
                return true;
            }

            // Check if user has edit permissions in the chapter's team
            var hasPermission = await _context.UserTeamRoles
                .Where(utr => utr.AppUserId == userId && utr.TeamId == chapter.TeamId)
                .Where(utr =>
                    // Team creators have all permissions
                    utr.Team.CreatorId == userId ||
                    // Team admins have all permissions
                    utr.Role == TeamRole.Admin ||
                    // Members with specific permission - UPDATED permission name
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanEditChapter"))
                )
                .AnyAsync();

            return hasPermission;
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
                        WordCount = c.Content != null ? c.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length : 0
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
                    Content = pendingChapter.Content
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
        /// Approve a pending chapter - UPDATED with additional authorization
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

                    .Include(pc => pc.Title)
                    .Include(pc => pc.Team)
                    .FirstOrDefaultAsync(pc => pc.Id == id);

                if (pendingChapter == null)
                {
                    return NotFound(new { message = "Pending chapter not found" });
                }

                // Additional authorization: Non-admin moderators can only approve chapters for teams they're part of
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    var canApprove = await _context.UserTeamRoles
                        .Where(utr => utr.AppUserId == user.Id && utr.TeamId == pendingChapter.TeamId)
                        .Where(utr => utr.Role == TeamRole.Admin)
                        .AnyAsync();

                    if (!canApprove)
                    {
                        return Forbid("You can only approve chapters for teams where you are an admin");
                    }
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
                        Content = pendingChapter.Content
                    };

                    _context.Chapters.Add(chapter);
                    await _context.SaveChangesAsync(); // Save to get the ID

                    // Remove the pending chapter
                    _context.PendingChapters.Remove(pendingChapter);
                    await _context.SaveChangesAsync();

                    // Write change log entry
                    var approveLog = new TitleChangeLog
                    {
                        TitleId = pendingChapter.TitleId,
                        UpdatedByUserId = pendingChapter.UpdatedByUserId,
                        ReviewedByUserId = user.Id,
                        CreatedAt = pendingChapter.CreatedDate,
                        ReviewedAt = DateTime.UtcNow,
                        ChangeType = "Add Chapter",
                        OldValue = "",
                        NewValue = $"Ch.{pendingChapter.ChapterNumber} - {pendingChapter.Name}",
                        AdminComment = "Approved by admin",
                        Status = ChangeLogStatus.Approved,
                    };
                    _context.TitleChangeLogs.Add(approveLog);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Record trust approval
                    await _trustService.RecordApprovalAsync(pendingChapter.UpdatedByUserId, TrustActionType.AddChapter);

                    // Load the chapter with all relationships for DTO mapping
                    var fullChapter = await _context.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .FirstOrDefaultAsync(c => c.Id == chapter.Id);

                    var result = ChapterMapper.ToDTO(fullChapter);

                    _logger.LogInformation("Chapter approved by {UserName}: {ChapterName} for {TitleName}",
                        user.UserName, chapter.Name, pendingChapter.Title.OriginalTitle);

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
                        Content = pendingChapter.Content
                    };

                    _context.RejectedChapters.Add(rejectedChapter);
                    await _context.SaveChangesAsync(); // Save to get the ID

                    // Remove the pending chapter
                    _context.PendingChapters.Remove(pendingChapter);
                    await _context.SaveChangesAsync();

                    // Record rejection → resets AddChapter trust counter
                    await _trustService.RecordRejectionAsync(pendingChapter.UpdatedByUserId, TrustActionType.AddChapter);

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
                string decodedTitleName = Uri.UnescapeDataString(titleName);

                // Support slug format "title-name-{id}" as well as plain name
                var (slugId, _) = ParseSlug(decodedTitleName);

                Chapter? chapter = null;

                // Try to parse chapterName as a number (used when chapter has no name)
                float? chapterNumFallback = null;
                if (float.TryParse(chapterName, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsedNum))
                    chapterNumFallback = parsedNum;

                if (slugId.HasValue)
                {
                    // Slug lookup — fast, by ID; match by Name first
                    chapter = await _context.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .FirstOrDefaultAsync(c =>
                            c.TitleId == slugId.Value &&
                            c.Name == chapterName &&
                            c.VolumeNumber == volume &&
                            c.TeamId == teamId);

                    // Fallback: chapter has empty name — match by ChapterNumber instead
                    if (chapter == null && chapterNumFallback.HasValue)
                    {
                        chapter = await _context.Chapters
                            .Include(c => c.Title)
                            .Include(c => c.Team)
                            .FirstOrDefaultAsync(c =>
                                c.TitleId == slugId.Value &&
                                (c.Name == null || c.Name == "") &&
                                c.ChapterNumber == chapterNumFallback.Value &&
                                c.VolumeNumber == volume &&
                                c.TeamId == teamId);
                    }
                }

                if (chapter == null)
                {
                    // Fallback: legacy plain-name lookup
                    chapter = await _context.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .FirstOrDefaultAsync(c =>
                            c.Title.OriginalTitle == decodedTitleName &&
                            c.Name == chapterName &&
                            c.VolumeNumber == volume &&
                            c.TeamId == teamId);
                }

                // Last resort: numeric fallback for plain-name titles with unnamed chapters
                if (chapter == null && chapterNumFallback.HasValue)
                {
                    chapter = await _context.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .FirstOrDefaultAsync(c =>
                            c.Title.OriginalTitle == decodedTitleName &&
                            (c.Name == null || c.Name == "") &&
                            c.ChapterNumber == chapterNumFallback.Value &&
                            c.VolumeNumber == volume &&
                            c.TeamId == teamId);
                }

                if (chapter == null)
                    return NotFound(new { message = "Chapter not found" });

                if (!chapter.Title.IsAvailable)
                    return NotFound(new { message = "Title is not available" });

                var chapterDto = ChapterMapper.ToDTO(chapter);
                await EnrichWithAdjacentChapters(chapterDto);

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                    await LogChapterView(chapter.Id, user.Id);

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

        // Helper method to add adjacent chapter information
        private async Task EnrichWithAdjacentChapters(ChapterDTO chapterDto)
        {
            var title = await _context.Titles
                .Include(t => t.Chapters)
                .FirstOrDefaultAsync(t => t.Id == chapterDto.TitleId);

            if (title == null) return;

            var orderedChapters = title.Chapters
                .OrderBy(c => c.VolumeNumber)
                .ThenBy(c => c.ChapterNumber)
                .ToList();

            int currentIndex = orderedChapters.FindIndex(c => c.Id == chapterDto.Id);
            if (currentIndex == -1) return;

            if (currentIndex < orderedChapters.Count - 1)
            {
                var nextChapter = orderedChapters[currentIndex + 1];
                chapterDto.NextChapterId = nextChapter.Id;
                chapterDto.NextChapterName = nextChapter.Name;
                chapterDto.NextChapterVolume = nextChapter.VolumeNumber;
                chapterDto.NextChapterTeamId = nextChapter.TeamId;
            }

            if (currentIndex > 0)
            {
                var prevChapter = orderedChapters[currentIndex - 1];
                chapterDto.PreviousChapterId = prevChapter.Id;
                chapterDto.PreviousChapterName = prevChapter.Name;
                chapterDto.PreviousChapterVolume = prevChapter.VolumeNumber;
                chapterDto.PreviousChapterTeamId = prevChapter.TeamId;
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

                    // Award XP for reading a new chapter
                    var reader = await _context.Users.FindAsync(userId);
                    if (reader != null)
                    {
                        reader.XpPoints += 3;
                        reader.UserLevel = AppUser.ComputeLevel(reader.XpPoints);
                    }

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
            public string Content { get; set; } = string.Empty;
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
        // Add these methods to your TitlesController.cs

        /// <summary>
        /// Get user's uploaded titles - UPDATED with proper filtering
        /// GET: api/Titles/UserTitles
        /// </summary>
        [HttpGet("UserTitles")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetUserTitles()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Get titles user created or has edit permissions for
                var userTitles = await _context.Titles
                    .Where(t =>
                        // User created the title
                        t.CreatedByUserId == user.Id ||
                        // Or user has edit permissions in any of the title's teams
                        t.Teams.Any(team =>
                            _context.UserTeamRoles.Any(utr =>
                                utr.AppUserId == user.Id &&
                                utr.TeamId == team.Id &&
                                (utr.Role == TeamRole.Admin ||
                                 (utr.Role == TeamRole.Member &&
                                  utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanEditTitle")))
                            )
                        )
                    )
                    .Include(t => t.Categories)
                    .Include(t => t.Teams)
                    .Include(t => t.Chapters)
                    .Select(t => new
                    {
                        t.Id,
                        t.OriginalTitle,
                        t.EnglishTitle,
                        t.CoverImagePath,
                        t.StatusTitle,
                        t.IsAvailable,
                        ChapterCount = t.Chapters.Count(),
                        LastUpdated = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            (DateTime?)null,
                        Teams = t.Teams.Select(team => team.Name).ToList(),
                        CanEdit = t.CreatedByUserId == user.Id // Flag to indicate if user created it
                    })
                    .ToListAsync();

                return Ok(userTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user titles");
                return StatusCode(500, new { message = "Error retrieving user titles" });
            }
        }

        /// <summary>
        /// Get user's chapters
        /// GET: api/Titles/UserChapters
        /// </summary>
        [HttpGet("UserChapters")]
        [Authorize]
        public async Task<ActionResult<object>> GetUserChapters()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var userChapters = await _context.Chapters
                    .Where(c => c.UpdatedByUserId == user.Id)
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .ToListAsync();

                var pendingChapters = await _context.PendingChapters
                    .Where(c => c.UpdatedByUserId == user.Id)
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .ToListAsync();

                var rejectedChapters = await _context.RejectedChapters
                    .Where(c => c.UpdatedByUserId == user.Id)
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .ToListAsync();

                var result = new
                {
                    ApprovedChapters = userChapters.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.VolumeNumber,
                        c.ChapterNumber,
                        TitleName = c.Title.OriginalTitle,
                        TeamName = c.Team.Name,
                        c.ReleaseDate
                    }),
                    PendingChapters = pendingChapters.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.VolumeNumber,
                        c.ChapterNumber,
                        TitleName = c.Title.OriginalTitle,
                        TeamName = c.Team.Name,
                        c.CreatedDate
                    }),
                    RejectedChapters = rejectedChapters.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.VolumeNumber,
                        c.ChapterNumber,
                        TitleName = c.Title.OriginalTitle,
                        TeamName = c.Team.Name,
                        c.CreatedDate
                    })
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user chapters");
                return StatusCode(500, new { message = "Error retrieving user chapters" });
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
                        .ThenInclude(c => c.Views)
                    .Include(t => t.Ratings)
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
                            (DateTime?)null,
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => (double)r.Value) : 0.0,
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count(),
                        StatusTitle = t.StatusTitle ?? "inproces",
                        StatusTranslation = t.StatusTranslation ?? "",
                        AgeRestriction = t.AgeRestriction
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

                // Take top 60 by popularity score, then re-sort by most recent chapter date
                // so the carousel shows popular titles that have been actively updated.
                // Fanfics (TitleCategory.Fanfic = 3) are excluded from the public carousel.
                var popularPool = await _context.Titles
                    .Where(t => t.IsAvailable && t.TitleCategory != TitleCategory.Fanfic)
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
                        LastChapterDate = t.Chapters.Any()
                            ? t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate
                            : DateTime.MinValue,
                        LatestChapterNumber = t.Chapters.Any()
                            ? (double)t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ChapterNumber
                            : 0,
                        PopularityScore = (t.Chapters.Count() * 2) +
                                          (t.Chapters.SelectMany(c => c.Views).Count() * 0.1) +
                                          (t.Ratings.Any() ? t.Ratings.Average(r => r.Value) * 10 : 0) +
                                          (t.Bookmarks.Count() * 5)
                    })
                    .OrderByDescending(x => x.PopularityScore)
                    .Take(60)
                    .ToListAsync();

                // Re-sort the popular pool: most recently updated first
                var result = popularPool
                    .OrderByDescending(x => x.LastChapterDate)
                    .Take(20)
                    .Select(item => new TitleListDto
                    {
                        Id = item.Title.Id,
                        OriginalTitle = item.Title.OriginalTitle ?? "Unknown Title",
                        EnglishTitle = item.Title.EnglishTitle ?? item.Title.OriginalTitle ?? "Unknown Title",
                        CoverImagePath = !string.IsNullOrEmpty(item.Title.CoverImagePath) ? item.Title.CoverImagePath : "/img/logo.png",
                        Type = item.Title.Type,
                        TitleCategory = item.Title.TitleCategory,
                        LatestChapter = item.LatestChapterNumber > 0
                            ? $"Ch. {item.LatestChapterNumber}"
                            : null,
                        LatestChapterNumber = item.LatestChapterNumber,
                        LastUpdated = item.LastChapterDate != DateTime.MinValue ? item.LastChapterDate : null,
                        ChapterCount = item.ChapterCount,
                        AverageRating = item.AverageRating,
                        BookmarkCount = item.BookmarkCount,
                        ReleaseDate = item.Title.ReleaseDate ?? "Unknown"
                    }).ToList();

                _logger.LogInformation($"Returning {result.Count} popular titles for carousel");
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

                // OPTIMIZED: Split into multiple efficient queries instead of one massive JOIN

                // 1. Get the basic title with only necessary navigation properties
                var title = await _context.Titles
                    .Where(t => t.IsAvailable && t.OriginalTitle == decodedTitle)
                    .Include(t => t.Teams)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .FirstOrDefaultAsync();

                if (title == null)
                {
                    _logger.LogWarning($"Title not found: {decodedTitle}");
                    return NotFound(new { message = "Title not found" });
                }

                // 2. Get aggregated statistics with separate efficient queries
                var titleId = title.Id;

                var chapterCount = await _context.Chapters
                    .Where(c => c.TitleId == titleId)
                    .CountAsync();

                var latestChapterNumber = await _context.Chapters
                    .Where(c => c.TitleId == titleId)
                    .MaxAsync(c => (int?)c.ChapterNumber) ?? 0;

                var ratingStats = await _context.Ratings
                    .Where(r => r.TitleId == titleId)
                    .GroupBy(r => r.TitleId)
                    .Select(g => new
                    {
                        Average = g.Average(r => (double)r.Value),
                        Count = g.Count()
                    })
                    .FirstOrDefaultAsync();

                var bookmarkCount = await _context.Bookmarks
                    .Where(b => b.TitleId == titleId)
                    .CountAsync();

                var viewCount = await _context.ChapterViews
                    .Where(cv => _context.Chapters
                        .Where(c => c.TitleId == titleId)
                        .Select(c => c.Id)
                        .Contains(cv.ChapterId))
                    .CountAsync();

                var lastUpdated = await _context.Chapters
                    .Where(c => c.TitleId == titleId)
                    .OrderByDescending(c => c.ReleaseDate)
                    .Select(c => (DateTime?)c.ReleaseDate)
                    .FirstOrDefaultAsync();

                // 3. Build the DTO
                var titleDto = new TitleDetailDto
                {
                    Id = title.Id,
                    OriginalTitle = title.OriginalTitle,
                    EnglishTitle = title.EnglishTitle ?? title.OriginalTitle,
                    Description = title.Description ?? "",
                    CoverImagePath = !string.IsNullOrEmpty(title.CoverImagePath) ? title.CoverImagePath : "/img/logo.png",
                    BackgroundImagePath = title.BackgroundImagePath,
                    Type = title.Type,
                    StatusTitle = title.StatusTitle ?? "Unknown",
                    StatusTranslation = title.StatusTranslation ?? "Unknown",
                    ReleaseDate = title.ReleaseDate ?? "Unknown",
                    AgeRestriction = title.AgeRestriction,
                    ChapterCount = chapterCount,
                    LatestChapter = latestChapterNumber > 0 ? latestChapterNumber.ToString() : "No chapters",
                    AverageRating = ratingStats?.Average ?? 0.0,
                    RatingCount = ratingStats?.Count ?? 0,
                    BookmarkCount = bookmarkCount,
                    ViewCount = viewCount,
                    LastUpdated = lastUpdated,
                    Teams = title.Teams.Select(team => new TeamSimpleDto
                    {
                        Id = team.Id,
                        Name = team.Name,
                        AvatarImagePath = team.AvatarImagePath,
                        BackgroundImagePath = team.BackgroundImagePath
                    }).ToList(),
                    Authors = title.Authors.Select(author => author.Name).ToList(),
                    Artists = title.Artists.Select(artist => artist.Name).ToList(),
                    Categories = title.Categories.Select(cat => cat.Name).ToList(),
                    Tags = title.Tags.Select(tag => tag.Name).ToList()
                };

                return Ok(titleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching title details for {EncodedTitle}: {Error}", encodedTitle, ex.Message);
                return StatusCode(500, new { message = "Error fetching title details", error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Titles/BySlug/{slug}
        // Resolves a slug like "naruto-42" → title with Id=42.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("BySlug/{slug}")]
        public async Task<ActionResult<TitleDetailDto>> GetTitleBySlug(string slug)
        {
            try
            {
                var decoded = Uri.UnescapeDataString(slug ?? "");
                var (titleId, _) = ParseSlug(decoded);

                if (titleId == null)
                    return BadRequest(new { message = "Invalid slug format — expected 'title-name-{id}'" });

                var title = await _context.Titles
                    .Where(t => t.IsAvailable && t.Id == titleId.Value)
                    .Include(t => t.Teams)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .FirstOrDefaultAsync();

                if (title == null)
                    return NotFound(new { message = "Title not found" });

                return Ok(await BuildTitleDetailDto(title));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching title by slug: {Slug}", slug);
                return StatusCode(500, new { message = "Error fetching title", error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Titles/CheckSimilarity?originalTitle=...&englishTitle=...&alternativeNames=...
        // Returns existing titles that are exact, near-exact or name-overlapping
        // with the supplied names. Used by the admin review panel.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("CheckSimilarity")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CheckSimilarity(
            [FromQuery] string? originalTitle = null,
            [FromQuery] string? englishTitle = null,
            [FromQuery] string? alternativeNames = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(originalTitle) && string.IsNullOrWhiteSpace(englishTitle))
                    return Ok(new { matches = new List<object>() });

                // Gather all candidate name tokens from the pending title
                var inputNames = new List<string>();
                if (!string.IsNullOrWhiteSpace(originalTitle)) inputNames.Add(originalTitle.Trim().ToLower());
                if (!string.IsNullOrWhiteSpace(englishTitle)) inputNames.Add(englishTitle.Trim().ToLower());
                if (!string.IsNullOrWhiteSpace(alternativeNames))
                    inputNames.AddRange(alternativeNames.Split(',', ';', '\n')
                        .Select(s => s.Trim().ToLower())
                        .Where(s => s.Length > 0));

                // Pull all approved titles (names only — fast query)
                var approvedTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .Select(t => new {
                        t.Id,
                        t.OriginalTitle,
                        t.EnglishTitle,
                        t.AlternativeNames
                    })
                    .ToListAsync();

                var matches = new List<object>();

                foreach (var existing in approvedTitles)
                {
                    // Build the name set for this approved title
                    var existingNames = new List<string>();
                    if (!string.IsNullOrWhiteSpace(existing.OriginalTitle))
                        existingNames.Add(existing.OriginalTitle.Trim().ToLower());
                    if (!string.IsNullOrWhiteSpace(existing.EnglishTitle))
                        existingNames.Add(existing.EnglishTitle.Trim().ToLower());
                    if (!string.IsNullOrWhiteSpace(existing.AlternativeNames))
                        existingNames.AddRange(existing.AlternativeNames.Split(',', ';', '\n')
                            .Select(s => s.Trim().ToLower())
                            .Where(s => s.Length > 0));

                    string? level = null;

                    // EXACT match (case-insensitive)
                    if (inputNames.Any(i => existingNames.Any(e => e == i)))
                        level = "exact";
                    // CONTAINS match (one name contains the other)
                    else if (inputNames.Any(i => existingNames.Any(e =>
                        e.Contains(i) || i.Contains(e))))
                        level = "similar";
                    // STARTS-WITH (first word matches)
                    else if (inputNames.Any(i =>
                    {
                        var firstWord = i.Split(' ')[0];
                        return firstWord.Length >= 4 && existingNames.Any(e => e.StartsWith(firstWord));
                    }))
                        level = "partial";

                    if (level != null)
                    {
                        matches.Add(new
                        {
                            id = existing.Id,
                            originalTitle = existing.OriginalTitle,
                            englishTitle = existing.EnglishTitle,
                            alternativeNames = existing.AlternativeNames,
                            matchLevel = level
                        });
                    }
                }

                // Sort: exact > similar > partial
                var ordered = matches
                    .OrderBy(m => ((dynamic)m).matchLevel == "exact" ? 0 :
                                  ((dynamic)m).matchLevel == "similar" ? 1 : 2)
                    .Take(10)
                    .ToList();

                return Ok(new { matches = ordered });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking title similarity");
                return StatusCode(500, new { message = "Error checking similarity", error = ex.Message });
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
        public async Task<ActionResult> UpdateReadingProgress([FromBody] UpdateProgressRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // For guest users, just return success without saving anything
                    return Ok(new
                    {
                        message = "Progress tracking not available for guest users",
                        isGuest = true
                    });
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

        // Controllers/TitlesController.cs - Add this CORRECTED catalog method

        /// <summary>
        /// Get catalog titles with filtering, sorting, and pagination
        /// GET: api/Titles/Catalog
        /// </summary>
        /// <summary>
        /// Full-text title search for global search.
        /// GET: api/Titles/Search?query=naruto
        /// </summary>
        [HttpGet("Search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchTitles([FromQuery] string query, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                return Ok(new List<object>());

            var q = query.Trim().ToLower();
            var results = await _context.Titles
                .Where(t => t.IsAvailable && (
                    t.EnglishTitle.ToLower().Contains(q) ||
                    t.OriginalTitle.ToLower().Contains(q) ||
                    t.AlternativeNames.ToLower().Contains(q)))
                .Include(t => t.Categories)
                .OrderBy(t => t.EnglishTitle.ToLower().StartsWith(q) ? 0 : 1)
                    .ThenBy(t => t.EnglishTitle)
                .Take(Math.Min(limit, 40))
                .Select(t => new
                {
                    t.Id,
                    t.EnglishTitle,
                    t.OriginalTitle,
                    t.CoverImagePath,
                    t.Type,
                    t.TitleCategory,
                    Categories = t.Categories.Select(c => c.Name).Take(3).ToList()
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Tag search for global search.
        /// GET: api/Titles/Tags/Search?query=action
        /// </summary>
        [HttpGet("Tags/Search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchTags([FromQuery] string query, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                return Ok(new List<object>());

            var q = query.Trim().ToLower();
            var tags = await _context.Tags
                .Where(t => t.Name.ToLower().Contains(q))
                .OrderBy(t => t.Name.ToLower().StartsWith(q) ? 0 : 1)
                    .ThenBy(t => t.Name)
                .Take(Math.Min(limit, 20))
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            return Ok(tags);
        }

        [HttpGet("Catalog")]
        public async Task<ActionResult<CatalogResponseDto>> GetCatalog(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 24,
            [FromQuery] string? search = null,
            [FromQuery] MangaType? type = null,
            [FromQuery] string? status = null,
            [FromQuery] string? translationStatus = null,
            [FromQuery] int? ageRestriction = null,
            [FromQuery] List<int>? categories = null,
            [FromQuery] List<int>? tags = null,
            [FromQuery] List<int>? formats = null,
            [FromQuery] List<int>? authors = null,
            [FromQuery] List<int>? artists = null,
            [FromQuery] List<int>? publishers = null,
            [FromQuery] List<int>? teams = null,
            [FromQuery] int? yearFrom = null,
            [FromQuery] int? yearTo = null,
            [FromQuery] string sortBy = "updated",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                _logger.LogInformation("Fetching catalog - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                    page, pageSize, search);

                // Validate pagination
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 24;
                if (pageSize > 100) pageSize = 100;

                // Start with base query - only available titles
                var query = _context.Titles
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .Include(t => t.Chapters)
                    .Where(t => t.IsAvailable);

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(t =>
                        t.OriginalTitle.ToLower().Contains(searchLower) ||
                        (t.EnglishTitle != null && t.EnglishTitle.ToLower().Contains(searchLower)) ||
                        (t.AlternativeNames != null && t.AlternativeNames.ToLower().Contains(searchLower)) ||
                        (t.Description != null && t.Description.ToLower().Contains(searchLower))
                    );
                }

                // Type filter
                if (type.HasValue)
                {
                    query = query.Where(t => t.Type == type.Value);
                }

                // Status filters
                if (!string.IsNullOrWhiteSpace(status) && status != "all")
                {
                    query = query.Where(t => t.StatusTitle == status);
                }

                if (!string.IsNullOrWhiteSpace(translationStatus) && translationStatus != "all")
                {
                    query = query.Where(t => t.StatusTranslation == translationStatus);
                }

                // Age restriction filter
                if (ageRestriction.HasValue)
                {
                    query = query.Where(t => t.AgeRestriction == ageRestriction.Value);
                }

                // Category filter
                if (categories != null && categories.Any())
                {
                    query = query.Where(t => t.Categories.Any(c => categories.Contains(c.Id)));
                }

                // Tag filter
                if (tags != null && tags.Any())
                {
                    query = query.Where(t => t.Tags.Any(tag => tags.Contains(tag.Id)));
                }

                // Format filter
                if (formats != null && formats.Any())
                {
                    query = query.Where(t => t.Formats.Any(f => formats.Contains(f.Id)));
                }

                // Author filter
                if (authors != null && authors.Any())
                {
                    query = query.Where(t => t.Authors.Any(a => authors.Contains(a.Id)));
                }

                // Artist filter
                if (artists != null && artists.Any())
                {
                    query = query.Where(t => t.Artists.Any(a => artists.Contains(a.Id)));
                }

                // Publisher filter
                if (publishers != null && publishers.Any())
                {
                    query = query.Where(t => t.Publishers.Any(p => publishers.Contains(p.Id)));
                }

                // Team filter
                if (teams != null && teams.Any())
                {
                    query = query.Where(t => t.Teams.Any(team => teams.Contains(team.Id)));
                }

                // Year range filter - ReleaseDate is a string in your model
                if (yearFrom.HasValue || yearTo.HasValue)
                {
                    if (yearFrom.HasValue && yearTo.HasValue)
                    {
                        query = query.Where(t =>
                            t.ReleaseDate != null &&
                            int.Parse(t.ReleaseDate.Substring(0, 4)) >= yearFrom.Value &&
                            int.Parse(t.ReleaseDate.Substring(0, 4)) <= yearTo.Value);
                    }
                    else if (yearFrom.HasValue)
                    {
                        query = query.Where(t =>
                            t.ReleaseDate != null &&
                            int.Parse(t.ReleaseDate.Substring(0, 4)) >= yearFrom.Value);
                    }
                    else if (yearTo.HasValue)
                    {
                        query = query.Where(t =>
                            t.ReleaseDate != null &&
                            int.Parse(t.ReleaseDate.Substring(0, 4)) <= yearTo.Value);
                    }
                }

                // Get total count before sorting/paging
                var totalCount = await query.CountAsync();

                // Sorting - Note: No direct rating/view fields on Title, using chapter count for now
                query = sortBy.ToLower() switch
                {
                    "title" => sortOrder == "asc"
                        ? query.OrderBy(t => t.OriginalTitle)
                        : query.OrderByDescending(t => t.OriginalTitle),

                    "chapters" => sortOrder == "asc"
                        ? query.OrderBy(t => t.Chapters.Count)
                        : query.OrderByDescending(t => t.Chapters.Count),

                    _ => sortOrder == "asc" // "updated" or default - use latest chapter date
                        ? query.OrderBy(t => t.Chapters.Any()
                            ? t.Chapters.Max(c => c.ReleaseDate)
                            : DateTime.MinValue)
                        : query.OrderByDescending(t => t.Chapters.Any()
                            ? t.Chapters.Max(c => c.ReleaseDate)
                            : DateTime.MinValue)
                };

                // Get title IDs for this page
                var titleIds = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => t.Id)
                    .ToListAsync();

                // Get the full titles with navigation properties
                var titles = await _context.Titles
                    .Where(t => titleIds.Contains(t.Id))
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .Include(t => t.Chapters)
                    .ToListAsync();

                // Calculate stats for each title (similar to your TitleDetailDto approach)
                var items = new List<TitleCatalogDto>();

                foreach (var title in titles)
                {
                    // Calculate rating stats
                    var ratingStats = await _context.Ratings
                        .Where(r => r.TitleId == title.Id)
                        .GroupBy(r => r.TitleId)
                        .Select(g => new { Average = g.Average(r => (double)r.Value), Count = g.Count() })
                        .FirstOrDefaultAsync();

                    // Calculate bookmark count
                    var bookmarkCount = await _context.Bookmarks
                        .Where(b => b.TitleId == title.Id)
                        .CountAsync();

                    // Calculate view count
                    var viewCount = await _context.ChapterViews
                        .Where(cv => _context.Chapters
                            .Where(c => c.TitleId == title.Id)
                            .Select(c => c.Id)
                            .Contains(cv.ChapterId))
                        .CountAsync();

                    // Get latest chapter info
                    var latestChapter = title.Chapters
                        .OrderByDescending(c => c.ReleaseDate)
                        .FirstOrDefault();

                    var catalogDto = new TitleCatalogDto
                    {
                        Id = title.Id,
                        OriginalTitle = title.OriginalTitle ?? "Unknown Title",
                        EnglishTitle = title.EnglishTitle ?? title.OriginalTitle ?? "",
                        AlternativeNames = title.AlternativeNames,
                        CoverImagePath = !string.IsNullOrEmpty(title.CoverImagePath) ? title.CoverImagePath : "/img/logo.png",
                        BackgroundImagePath = title.BackgroundImagePath,
                        Type = title.Type,
                        StatusTitle = title.StatusTitle ?? "Unknown",
                        StatusTranslation = title.StatusTranslation ?? "Unknown",
                        AgeRestriction = title.AgeRestriction,
                        Description = title.Description != null && title.Description.Length > 200
                            ? title.Description.Substring(0, 200) + "..."
                            : title.Description ?? "",
                        LatestChapter = latestChapter != null
                            ? $"Ch. {latestChapter.ChapterNumber}"
                            : null,
                        ChapterCount = title.Chapters.Count,
                        ReleaseDate = title.ReleaseDate ?? "Unknown",
                        LastUpdated = latestChapter?.ReleaseDate,
                        AverageRating = ratingStats?.Average ?? 0.0,
                        RatingCount = ratingStats?.Count ?? 0,
                        BookmarkCount = bookmarkCount,
                        ViewCount = viewCount,
                        Authors = title.Authors.Select(a => a.Name).ToList(),
                        Artists = title.Artists.Select(a => a.Name).ToList(),
                        Publishers = title.Publishers.Select(p => p.Name).ToList(),
                        Teams = title.Teams.Select(t => t.Name).ToList(),
                        Categories = title.Categories.Select(c => c.Name).ToList(),
                        Tags = title.Tags.Select(tag => tag.Name).ToList(),
                        Formats = title.Formats.Select(f => f.Name).ToList()
                    };

                    items.Add(catalogDto);
                }

                // Sort items to match the original query order
                var orderedItems = sortBy.ToLower() switch
                {
                    "title" => sortOrder == "asc"
                        ? items.OrderBy(t => t.OriginalTitle).ToList()
                        : items.OrderByDescending(t => t.OriginalTitle).ToList(),
                    "chapters" => sortOrder == "asc"
                        ? items.OrderBy(t => t.ChapterCount).ToList()
                        : items.OrderByDescending(t => t.ChapterCount).ToList(),
                    _ => sortOrder == "asc"
                        ? items.OrderBy(t => t.LastUpdated ?? DateTime.MinValue).ToList()
                        : items.OrderByDescending(t => t.LastUpdated ?? DateTime.MinValue).ToList()
                };

                var response = new CatalogResponseDto
                {
                    Items = orderedItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                _logger.LogInformation("Catalog query successful - Found {Count} titles, Page {Page}/{TotalPages}",
                    totalCount, page, response.TotalPages);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching catalog: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching catalog", error = ex.Message });
            }
        }

        /// <summary>
        /// Get filter options with counts
        /// GET: api/Titles/Catalog/FilterOptions
        /// </summary>
        [HttpGet("Catalog/FilterOptions")]
        public async Task<ActionResult<CatalogFilterOptionsDto>> GetCatalogFilterOptions()
        {
            try
            {
                var availableTitles = _context.Titles.Where(t => t.IsAvailable);

                var filterOptions = new CatalogFilterOptionsDto
                {
                    Authors = await _context.Authors
                        .Where(a => availableTitles.Any(t => t.Authors.Contains(a)))
                        .Select(a => new FilterOptionDto
                        {
                            Id = a.Id,
                            Name = a.Name,
                            Count = availableTitles.Count(t => t.Authors.Contains(a))
                        })
                        .OrderBy(a => a.Name)
                        .ToListAsync(),

                    Artists = await _context.Artists
                        .Where(a => availableTitles.Any(t => t.Artists.Contains(a)))
                        .Select(a => new FilterOptionDto
                        {
                            Id = a.Id,
                            Name = a.Name,
                            Count = availableTitles.Count(t => t.Artists.Contains(a))
                        })
                        .OrderBy(a => a.Name)
                        .ToListAsync(),

                    Publishers = await _context.Publishers
                        .Where(p => availableTitles.Any(t => t.Publishers.Contains(p)))
                        .Select(p => new FilterOptionDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Count = availableTitles.Count(t => t.Publishers.Contains(p))
                        })
                        .OrderBy(p => p.Name)
                        .ToListAsync(),

                    Teams = await _context.Teams
                        .Where(t => availableTitles.Any(title => title.Teams.Contains(t)))
                        .Select(t => new FilterOptionDto
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Count = availableTitles.Count(title => title.Teams.Contains(t))
                        })
                        .OrderBy(t => t.Name)
                        .ToListAsync(),

                    Categories = await _context.Categories
                        .Where(c => availableTitles.Any(t => t.Categories.Contains(c)))
                        .Select(c => new FilterOptionDto
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Count = availableTitles.Count(t => t.Categories.Contains(c))
                        })
                        .OrderBy(c => c.Name)
                        .ToListAsync(),

                    Tags = await _context.Tags
                        .Where(tag => availableTitles.Any(t => t.Tags.Contains(tag)))
                        .Select(tag => new FilterOptionDto
                        {
                            Id = tag.Id,
                            Name = tag.Name,
                            Count = availableTitles.Count(t => t.Tags.Contains(tag))
                        })
                        .OrderBy(tag => tag.Name)
                        .ToListAsync(),

                    Formats = await _context.Formats
                        .Where(f => availableTitles.Any(t => t.Formats.Contains(f)))
                        .Select(f => new FilterOptionDto
                        {
                            Id = f.Id,
                            Name = f.Name,
                            Count = availableTitles.Count(t => t.Formats.Contains(f))
                        })
                        .OrderBy(f => f.Name)
                        .ToListAsync()
                };

                return Ok(filterOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching filter options: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching filter options", error = ex.Message });
            }
        }

        public class UpdateProgressRequest
        {
            public int TitleId { get; set; }
            public int ChapterNumber { get; set; }
        }

        // ── Slug helpers ─────────────────────────────────────────────────────
        // Slug format: "{title-name}-{id}"  e.g. "naruto-42", "my-hero-academia-123"
        private static (int? id, string namePart) ParseSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return (null, slug ?? "");
            var lastDash = slug.LastIndexOf('-');
            if (lastDash > 0)
            {
                var suffix = slug.Substring(lastDash + 1);
                if (int.TryParse(suffix, out var id) && id > 0)
                    return (id, slug.Substring(0, lastDash));
            }
            return (null, slug);
        }

        private async Task<TitleDetailDto> BuildTitleDetailDto(
            FallenFaction.Server.Data.Models.Title title)
        {
            var id = title.Id;

            var chapterCount = await _context.Chapters.Where(c => c.TitleId == id).CountAsync();
            var latestNum = await _context.Chapters.Where(c => c.TitleId == id)
                                   .MaxAsync(c => (int?)c.ChapterNumber) ?? 0;
            var ratingStats = await _context.Ratings.Where(r => r.TitleId == id)
                                   .GroupBy(r => r.TitleId)
                                   .Select(g => new { Avg = g.Average(r => (double)r.Value), Cnt = g.Count() })
                                   .FirstOrDefaultAsync();
            var bookmarks = await _context.Bookmarks.Where(b => b.TitleId == id).CountAsync();
            var views = await _context.ChapterViews
                                   .Where(cv => _context.Chapters.Where(c => c.TitleId == id)
                                       .Select(c => c.Id).Contains(cv.ChapterId))
                                   .CountAsync();
            var lastUpdated = await _context.Chapters.Where(c => c.TitleId == id)
                                   .OrderByDescending(c => c.ReleaseDate)
                                   .Select(c => (DateTime?)c.ReleaseDate)
                                   .FirstOrDefaultAsync();

            return new TitleDetailDto
            {
                Id = title.Id,
                OriginalTitle = title.OriginalTitle,
                EnglishTitle = title.EnglishTitle ?? title.OriginalTitle,
                Description = title.Description ?? "",
                CoverImagePath = !string.IsNullOrEmpty(title.CoverImagePath) ? title.CoverImagePath : "/img/logo.png",
                BackgroundImagePath = title.BackgroundImagePath,
                Type = title.Type,
                StatusTitle = title.StatusTitle ?? "Unknown",
                StatusTranslation = title.StatusTranslation ?? "Unknown",
                ReleaseDate = title.ReleaseDate ?? "Unknown",
                AgeRestriction = title.AgeRestriction,
                ChapterCount = chapterCount,
                LatestChapter = latestNum > 0 ? latestNum.ToString() : "No chapters",
                AverageRating = ratingStats?.Avg ?? 0.0,
                RatingCount = ratingStats?.Cnt ?? 0,
                BookmarkCount = bookmarks,
                ViewCount = views,
                LastUpdated = lastUpdated,
                Teams = title.Teams.Select(t => new TeamSimpleDto { Id = t.Id, Name = t.Name, AvatarImagePath = t.AvatarImagePath, BackgroundImagePath = t.BackgroundImagePath, IsSystemTeam = t.IsSystemTeam }).ToList(),
                Authors = title.Authors.Select(a => a.Name).ToList(),
                Artists = title.Artists.Select(a => a.Name).ToList(),
                Categories = title.Categories.Select(c => c.Name).ToList(),
                Tags = title.Tags.Select(t => t.Name).ToList()
            };
        }
    }
}