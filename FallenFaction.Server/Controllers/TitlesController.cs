// Controllers/TitlesController.cs
using System.ComponentModel.DataAnnotations;
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
                    Content = request.Content,
                    CharacterCount = request.Content?.Length ?? 0   // ← ADD THIS LINE
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
                    WordCount = request.Content?.Length / 5 ?? 0
                };

                _logger.LogInformation("Chapter created by user {UserName} for title {TitleName}, team {TeamName}",
                    user.UserName, title.OriginalTitle, team?.Name);

                return CreatedAtAction(nameof(GetPendingChapter), new { id = pendingChapter.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chapter for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error creating chapter" });
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
        /// <summary>
        /// Category-aware chapter edit permission check.
        /// IMPORTANT: chapter must be loaded with .Include(c => c.Title) for category rules to work.
        /// </summary>
        private async Task<bool> CanUserEditChapter(string userId, Chapter chapter)
        {
            // Admins can edit everything regardless of category
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                return true;

            var category = chapter.Title?.TitleCategory ?? TitleCategory.Translation;

            switch (category)
            {
                // ── AI/TL: admin-only (non-admins already blocked above) ───────
                case TitleCategory.AITranslation:
                    return false;

                // ── Original / Fanfic: only the title creator can edit ─────────
                case TitleCategory.Original:
                case TitleCategory.Fanfic:
                    return chapter.Title?.CreatedByUserId == userId;

                // ── Translation: team-based permission ────────────────────────
                case TitleCategory.Translation:
                default:
                    // The user who last submitted the chapter retains edit access
                    if (chapter.UpdatedByUserId == userId)
                        return true;

                    // Must have CanEditChapter permission within the chapter's team
                    return await _context.UserTeamRoles
                        .Where(utr => utr.AppUserId == userId && utr.TeamId == chapter.TeamId)
                        .Where(utr =>
                            utr.Team.CreatorId == userId ||
                            utr.Role == TeamRole.Admin ||
                            (utr.Role == TeamRole.Member &&
                             utr.UserTeamRolePermissions.Any(p =>
                                 p.UserTeamPermission.PermissionName == "CanEditChapter")))
                        .AnyAsync();
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
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.VolumeNumber,
                        c.ChapterNumber,
                        TitleName = c.Title != null
                            ? c.Title.OriginalTitle
                            : (c.PendingTitle != null ? c.PendingTitle.OriginalTitle : "Unknown"),
                        c.TitleId,
                        c.PendingTitleId,
                        IsTitleApproved = c.TitleId.HasValue,
                        TeamName = c.Team.Name,
                        c.CreatedDate,
                        UpdatedByUserName = c.UpdatedByUser.UserName,
                        // Approximate word count from pre-stored character count (no Content load).
                        // CharacterCount / 5.5 ≈ average English words. 0 if content not yet set.
                        WordCount = c.CharacterCount > 0 ? (int)(c.CharacterCount / 5.5) : 0,
                        c.OriginalChapterId
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
                    .Include(c => c.PendingTitle)
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
                    PendingTitleId = pendingChapter.PendingTitleId,
                    TitleName = pendingChapter.Title != null ? pendingChapter.Title.OriginalTitle : (pendingChapter.PendingTitle != null ? pendingChapter.PendingTitle.OriginalTitle : "Unknown"),
                    IsTitleApproved = pendingChapter.TitleId.HasValue,
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

                if (!pendingChapter.TitleId.HasValue)
                {
                    return BadRequest(new { message = "Cannot approve this chapter because its title is still pending approval" });
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
                    Chapter resultChapter;

                    if (pendingChapter.OriginalChapterId.HasValue)
                    {
                        // This is an edit of an existing chapter — update it in-place
                        var original = await _context.Chapters
                            .Include(c => c.Title)
                            .Include(c => c.Team)
                            .FirstOrDefaultAsync(c => c.Id == pendingChapter.OriginalChapterId.Value);

                        if (original == null)
                            return NotFound(new { message = "Original chapter no longer exists" });

                        string oldName = original.Name;
                        original.Name = pendingChapter.Name;
                        original.VolumeNumber = pendingChapter.VolumeNumber;
                        original.ChapterNumber = pendingChapter.ChapterNumber;
                        original.TeamId = pendingChapter.TeamId;
                        original.Content = pendingChapter.Content;
                        original.LastUpdatedAt = DateTime.UtcNow;
                        original.UpdatedByUserId = user.Id;

                        _context.PendingChapters.Remove(pendingChapter);
                        await _context.SaveChangesAsync();

                        // Mark any pending change-log entry as Approved
                        var pendingLog = await _context.TitleChangeLogs
                            .Where(l => l.TitleId == pendingChapter.TitleId
                                     && l.UpdatedByUserId == pendingChapter.UpdatedByUserId
                                     && l.ChangeType == "Edit Chapter"
                                     && l.Status == ChangeLogStatus.Pending)
                            .OrderByDescending(l => l.CreatedAt)
                            .FirstOrDefaultAsync();

                        if (pendingLog != null)
                        {
                            pendingLog.Status = ChangeLogStatus.Approved;
                            pendingLog.ReviewedByUserId = user.Id;
                            pendingLog.ReviewedAt = DateTime.UtcNow;
                            pendingLog.AdminComment = "Approved by admin";
                        }
                        else
                        {
                            _context.TitleChangeLogs.Add(new TitleChangeLog
                            {
                                TitleId = pendingChapter.TitleId.Value,
                                UpdatedByUserId = pendingChapter.UpdatedByUserId,
                                ReviewedByUserId = user.Id,
                                CreatedAt = pendingChapter.CreatedDate,
                                ReviewedAt = DateTime.UtcNow,
                                ChangeType = "Edit Chapter",
                                OldValue = $"Ch.{original.ChapterNumber} - {oldName}",
                                NewValue = $"Ch.{pendingChapter.ChapterNumber} - {pendingChapter.Name}",
                                AdminComment = "Approved by admin",
                                Status = ChangeLogStatus.Approved,
                            });
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        await _trustService.RecordApprovalAsync(pendingChapter.UpdatedByUserId, TrustActionType.AddChapter);

                        resultChapter = await _context.Chapters.Include(c => c.Title).Include(c => c.Team)
                            .FirstAsync(c => c.Id == original.Id);

                        _logger.LogInformation("Chapter edit approved by {Admin}: Ch.{Num} for {Title}",
                            user.UserName, original.ChapterNumber, pendingChapter.Title.OriginalTitle);
                    }
                    else
                    {
                        // New chapter submission — create it
                        var chapter = new Chapter
                        {
                            Name = pendingChapter.Name,
                            VolumeNumber = pendingChapter.VolumeNumber,
                            ChapterNumber = pendingChapter.ChapterNumber,
                            TitleId = pendingChapter.TitleId.Value,
                            TeamId = pendingChapter.TeamId,
                            CreatedDate = DateTime.UtcNow,
                            ReleaseDate = DateTime.UtcNow,
                            UpdatedByUserId = user.Id,
                            Content = pendingChapter.Content
                        };

                        _context.Chapters.Add(chapter);
                        await _context.SaveChangesAsync();

                        _context.PendingChapters.Remove(pendingChapter);
                        await _context.SaveChangesAsync();

                        _context.TitleChangeLogs.Add(new TitleChangeLog
                        {
                            TitleId = pendingChapter.TitleId.Value,
                            UpdatedByUserId = pendingChapter.UpdatedByUserId,
                            ReviewedByUserId = user.Id,
                            CreatedAt = pendingChapter.CreatedDate,
                            ReviewedAt = DateTime.UtcNow,
                            ChangeType = "Add Chapter",
                            OldValue = "",
                            NewValue = $"Ch.{pendingChapter.ChapterNumber} - {pendingChapter.Name}",
                            AdminComment = "Approved by admin",
                            Status = ChangeLogStatus.Approved,
                        });
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        await _trustService.RecordApprovalAsync(pendingChapter.UpdatedByUserId, TrustActionType.AddChapter);

                        resultChapter = await _context.Chapters.Include(c => c.Title).Include(c => c.Team)
                            .FirstAsync(c => c.Id == chapter.Id);

                        _logger.LogInformation("Chapter approved by {UserName}: {ChapterName} for {TitleName}",
                            user.UserName, chapter.Name, pendingChapter.Title.OriginalTitle);
                    }

                    var result = ChapterMapper.ToDTO(resultChapter);
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

        public class MassApproveRequest
        {
            public List<int> ChapterIds { get; set; } = new List<int>();
        }

        /// <summary>
        /// Mass approve pending chapters
        /// POST: api/Titles/chapters/pending/mass-approve
        /// </summary>
        [HttpPost("chapters/pending/mass-approve")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> MassApproveChapters([FromBody] MassApproveRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                if (request.ChapterIds == null || !request.ChapterIds.Any())
                    return BadRequest(new { message = "No chapter IDs provided" });

                var pendingChapters = await _context.PendingChapters
                    .Include(pc => pc.Title)
                    .Include(pc => pc.Team)
                    .Where(pc => request.ChapterIds.Contains(pc.Id))
                    .ToListAsync();

                if (!pendingChapters.Any())
                    return NotFound(new { message = "No valid pending chapters found" });

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    var teamIds = pendingChapters.Select(pc => pc.TeamId).Distinct().ToList();
                    foreach (var teamId in teamIds)
                    {
                        var canApprove = await _context.UserTeamRoles
                            .Where(utr => utr.AppUserId == user.Id && utr.TeamId == teamId && utr.Role == TeamRole.Admin)
                            .AnyAsync();
                        if (!canApprove) return Forbid($"You lack admin rights for team {teamId}");
                    }
                }

                int approvedCount = 0;

                // Pre-fetch all original chapters BEFORE opening the transaction.
                // Previously, FirstOrDefaultAsync ran inside the transaction loop, holding
                // write locks on Chapters/PendingChapters for 30+ seconds and starving
                // Featured/Popular/RecentUpdates queries (causing timeouts & deadlocks).
                var originalChapterIds = pendingChapters
                    .Where(pc => pc.OriginalChapterId.HasValue)
                    .Select(pc => pc.OriginalChapterId!.Value)
                    .Distinct()
                    .ToList();

                var originalChaptersMap = originalChapterIds.Any()
                    ? await _context.Chapters
                        .Where(c => originalChapterIds.Contains(c.Id))
                        .ToDictionaryAsync(c => c.Id)
                    : new Dictionary<int, Chapter>();

                // Process in chunks of 500. A single SaveChangesAsync for 4850 entities
                // generates a ~100 KB SQL batch that blocks the entire DB for 30+ seconds.
                // Each chunk commits independently so each transaction is short-lived.
                const int chunkSize = 500;
                var chunks = pendingChapters
                    .Where(pc => pc.TitleId.HasValue)
                    .Chunk(chunkSize);

                foreach (var chunk in chunks)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        foreach (var pendingChapter in chunk)
                        {
                            if (pendingChapter.OriginalChapterId.HasValue)
                            {
                                originalChaptersMap.TryGetValue(pendingChapter.OriginalChapterId.Value, out var original);
                                if (original != null)
                                {
                                    original.Name = pendingChapter.Name;
                                    original.VolumeNumber = pendingChapter.VolumeNumber;
                                    original.ChapterNumber = pendingChapter.ChapterNumber;
                                    original.TeamId = pendingChapter.TeamId;
                                    original.Content = pendingChapter.Content;
                                    original.LastUpdatedAt = DateTime.UtcNow;
                                    original.UpdatedByUserId = user.Id;

                                    _context.TitleChangeLogs.Add(new TitleChangeLog
                                    {
                                        TitleId = pendingChapter.TitleId!.Value,
                                        UpdatedByUserId = pendingChapter.UpdatedByUserId,
                                        ReviewedByUserId = user.Id,
                                        CreatedAt = pendingChapter.CreatedDate,
                                        ReviewedAt = DateTime.UtcNow,
                                        ChangeType = "Edit Chapter",
                                        OldValue = $"Ch.{original.ChapterNumber}",
                                        NewValue = $"Ch.{pendingChapter.ChapterNumber} - {pendingChapter.Name}",
                                        AdminComment = "Mass-Approved by admin",
                                        Status = ChangeLogStatus.Approved,
                                    });
                                }
                            }
                            else
                            {
                                var chapter = new Chapter
                                {
                                    Name = pendingChapter.Name,
                                    VolumeNumber = pendingChapter.VolumeNumber,
                                    ChapterNumber = pendingChapter.ChapterNumber,
                                    TitleId = pendingChapter.TitleId!.Value,
                                    TeamId = pendingChapter.TeamId,
                                    CreatedDate = DateTime.UtcNow,
                                    ReleaseDate = DateTime.UtcNow,
                                    UpdatedByUserId = user.Id,
                                    Content = pendingChapter.Content
                                };
                                _context.Chapters.Add(chapter);

                                _context.TitleChangeLogs.Add(new TitleChangeLog
                                {
                                    TitleId = pendingChapter.TitleId!.Value,
                                    UpdatedByUserId = pendingChapter.UpdatedByUserId,
                                    ReviewedByUserId = user.Id,
                                    CreatedAt = pendingChapter.CreatedDate,
                                    ReviewedAt = DateTime.UtcNow,
                                    ChangeType = "Add Chapter",
                                    OldValue = "",
                                    NewValue = $"Ch.{pendingChapter.ChapterNumber} - {pendingChapter.Name}",
                                    AdminComment = "Mass-Approved by admin",
                                    Status = ChangeLogStatus.Approved,
                                });

                                await _trustService.RecordApprovalAsync(pendingChapter.UpdatedByUserId, TrustActionType.AddChapter);
                            }

                            _context.PendingChapters.Remove(pendingChapter);
                            approvedCount++;
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                _logger.LogInformation("Mass-approved {Count} chapters by {User}", approvedCount, user.UserName);
                return Ok(new { message = $"Successfully mass-approved {approvedCount} chapters." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mass approving chapters");
                return StatusCode(500, new { message = "Error mass approving chapters" });
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
                    .Include(pc => pc.Title)
                    .FirstOrDefaultAsync(pc => pc.Id == id);

                if (pendingChapter == null)
                {
                    return NotFound(new { message = "Pending chapter not found" });
                }

                if (!pendingChapter.TitleId.HasValue)
                {
                    return BadRequest(new { message = "Cannot reject this chapter because its title is still pending approval" });
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
                        TitleId = pendingChapter.TitleId.Value,
                        TeamId = pendingChapter.TeamId,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedByUserId = user.Id,
                        Content = pendingChapter.Content
                    };

                    _context.RejectedChapters.Add(rejectedChapter);
                    await _context.SaveChangesAsync();

                    // If this was an edit, update the pending change-log entry to Rejected
                    bool isEdit = pendingChapter.OriginalChapterId.HasValue;
                    string changeType = isEdit ? "Edit Chapter" : "Add Chapter";

                    var pendingLog = await _context.TitleChangeLogs
                        .Where(l => l.TitleId == pendingChapter.TitleId
                                 && l.UpdatedByUserId == pendingChapter.UpdatedByUserId
                                 && l.ChangeType == changeType
                                 && l.Status == ChangeLogStatus.Pending)
                        .OrderByDescending(l => l.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (pendingLog != null)
                    {
                        pendingLog.Status = ChangeLogStatus.Rejected;
                        pendingLog.ReviewedByUserId = user.Id;
                        pendingLog.ReviewedAt = DateTime.UtcNow;
                        pendingLog.RejectionReason = request?.Reason;
                        pendingLog.AdminComment = "Rejected by admin";
                    }
                    else
                    {
                        _context.TitleChangeLogs.Add(new TitleChangeLog
                        {
                            TitleId = pendingChapter.TitleId.Value,
                            UpdatedByUserId = pendingChapter.UpdatedByUserId,
                            ReviewedByUserId = user.Id,
                            CreatedAt = pendingChapter.CreatedDate,
                            ReviewedAt = DateTime.UtcNow,
                            ChangeType = changeType,
                            OldValue = "",
                            NewValue = $"Ch.{pendingChapter.ChapterNumber} - {pendingChapter.Name}",
                            AdminComment = "Rejected by admin",
                            RejectionReason = request?.Reason,
                            Status = ChangeLogStatus.Rejected,
                        });
                    }

                    // Remove the pending chapter
                    _context.PendingChapters.Remove(pendingChapter);
                    await _context.SaveChangesAsync();

                    // Record rejection → resets AddChapter trust counter
                    await _trustService.RecordRejectionAsync(pendingChapter.UpdatedByUserId, TrustActionType.AddChapter);

                    await transaction.CommitAsync();

                    return Ok(new { message = isEdit ? "Chapter edit rejected" : "Chapter rejected successfully", reason = request?.Reason });
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
                // Verify the title exists and is available in the same round-trip
                var titleExists = await _context.Titles
                    .AsNoTracking()
                    .AnyAsync(t => t.Id == titleId && t.IsAvailable);

                if (!titleExists)
                    return NotFound(new { message = "Title not found" });

                // Project to DTO in SQL — Content is intentionally excluded from the
                // chapter list. It is fetched only by GetChapterByRoute/GetChapterForReading.
                var chapterDtos = await _context.Chapters
                    .AsNoTracking()
                    .Where(c => c.TitleId == titleId)
                    .OrderByDescending(c => c.VolumeNumber)
                    .ThenByDescending(c => c.ChapterNumber)
                    .Select(c => new ChapterDTO
                    {
                        Id = c.Id,
                        Name = c.Name ?? string.Empty,
                        VolumeNumber = c.VolumeNumber,
                        ChapterNumber = c.ChapterNumber,
                        TitleId = c.TitleId,
                        TitleName = string.Empty, // not needed on the list
                        TeamId = c.TeamId,
                        Team = c.Team != null ? new NameIdDTO
                        {
                            Id = c.Team.Id,
                            Name = c.Team.Name ?? string.Empty,
                            AvatarImagePath = c.Team.AvatarImagePath,
                            BackgroundImagePath = c.Team.BackgroundImagePath
                        } : null,
                        CreatedDate = c.CreatedDate,
                        ReleaseDate = c.ReleaseDate,
                        IsAILocked = c.IsAILocked,
                        CharacterCount = c.CharacterCount,
                        Content = string.Empty   // explicitly excluded from list
                    })
                    .ToListAsync();

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
        public async Task<ActionResult<ChapterDTO>> GetChapterByRoute(string titleName, string chapterName, int volume, int teamId, [FromQuery] int? page = null, [FromQuery] int? cid = null)
        {
            try
            {
                string decodedTitleName = Uri.UnescapeDataString(titleName);

                // Support slug format "title-name-{id}" as well as plain name
                var (slugId, _) = ParseSlug(decodedTitleName);

                Chapter? chapter = null;

                // Fast path: cid (chapter ID) is provided — used when two chapters share
                // the same name and would otherwise produce an identical URL.
                // TitleId must match the slug to prevent cross-title chapter access.
                if (cid.HasValue && slugId.HasValue)
                {
                    chapter = await _context.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .FirstOrDefaultAsync(c => c.Id == cid.Value && c.TitleId == slugId.Value);
                }

                // Try to parse chapterName as a number (used when chapter has no name)
                float? chapterNumFallback = null;
                if (float.TryParse(chapterName, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsedNum))
                    chapterNumFallback = parsedNum;

                if (chapter == null && slugId.HasValue)
                {
                    // Slug lookup — match by Name; ORDER BY ChapterNumber so that when
                    // multiple chapters share the same name the earliest one wins deterministically.
                    chapter = await _context.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .Where(c =>
                            c.TitleId == slugId.Value &&
                            c.Name == chapterName &&
                            c.VolumeNumber == volume &&
                            c.TeamId == teamId)
                        .OrderBy(c => c.ChapterNumber)
                        .FirstOrDefaultAsync();

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
                    // Fallback: legacy plain-name lookup; ORDER BY ChapterNumber for same reason.
                    chapter = await _context.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .Where(c =>
                            c.Title.OriginalTitle == decodedTitleName &&
                            c.Name == chapterName &&
                            c.VolumeNumber == volume &&
                            c.TeamId == teamId)
                        .OrderBy(c => c.ChapterNumber)
                        .FirstOrDefaultAsync();
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
            // Project only nav fields — avoids loading Content column for every chapter
            var orderedChapters = await _context.Chapters
                .Where(c => c.TitleId == chapterDto.TitleId)
                .OrderBy(c => c.VolumeNumber)
                .ThenBy(c => c.ChapterNumber)
                .Select(c => new {
                    c.Id,
                    c.Name,
                    c.ChapterNumber,
                    c.VolumeNumber,
                    c.TeamId
                })
                .ToListAsync();

            int currentIndex = orderedChapters.FindIndex(c => c.Id == chapterDto.Id);
            if (currentIndex == -1) return;

            if (currentIndex < orderedChapters.Count - 1)
            {
                var nextChapter = orderedChapters[currentIndex + 1];
                chapterDto.NextChapterId = nextChapter.Id;
                chapterDto.NextChapterName = nextChapter.Name;
                chapterDto.NextChapterNumber = nextChapter.ChapterNumber;
                chapterDto.NextChapterVolume = nextChapter.VolumeNumber;
                chapterDto.NextChapterTeamId = nextChapter.TeamId;
            }

            if (currentIndex > 0)
            {
                var prevChapter = orderedChapters[currentIndex - 1];
                chapterDto.PreviousChapterId = prevChapter.Id;
                chapterDto.PreviousChapterName = prevChapter.Name;
                chapterDto.PreviousChapterNumber = prevChapter.ChapterNumber;
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
            [Required, StringLength(255)]
            public string Name { get; set; } = string.Empty;

            [Range(0, 9999)]
            public int VolumeNumber { get; set; }

            [Range(0, 99999)]
            public int ChapterNumber { get; set; }

            [Range(1, int.MaxValue)]
            public int TeamId { get; set; }

            [Required, StringLength(100000, MinimumLength = 1, ErrorMessage = "Chapter content must be between 1 and 100,000 characters.")]
            public string Content { get; set; } = string.Empty;
        }

        public class RejectChapterRequest
        {
            [StringLength(1000)]
            public string? Reason { get; set; }
        }

        #endregion

        // ... [Rest of existing methods remain the same] ...

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
                        c.TitleId,
                        TitleName = c.Title.OriginalTitle ?? c.Title.EnglishTitle ?? string.Empty,
                        c.TeamId,
                        TeamName = c.Team != null ? c.Team.Name : string.Empty,
                        c.ReleaseDate
                    }),
                    PendingChapters = pendingChapters.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.VolumeNumber,
                        c.ChapterNumber,
                        c.TitleId,
                        TitleName = c.Title.OriginalTitle ?? c.Title.EnglishTitle ?? string.Empty,
                        c.TeamId,
                        TeamName = c.Team != null ? c.Team.Name : string.Empty,
                        c.CreatedDate
                    }),
                    RejectedChapters = rejectedChapters.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.VolumeNumber,
                        c.ChapterNumber,
                        c.TitleId,
                        TitleName = c.Title.OriginalTitle ?? c.Title.EnglishTitle ?? string.Empty,
                        c.TeamId,
                        TeamName = c.Team != null ? c.Team.Name : string.Empty,
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

                // FIXED: Use pure projection (no Include) so EF translates everything to SQL
                // instead of loading all Chapters+Views into memory (which caused timeouts
                // when a long mass-approve transaction was holding locks on the Chapters table).
                var featuredTitles = await _context.Titles
                    .AsNoTracking()
                    .Where(t => t.IsAvailable)
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
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).Select(c => c.ReleaseDate).FirstOrDefault() :
                            (DateTime?)null,
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => (double)r.Value) : 0.0,
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count(),
                        StatusTitle = t.StatusTitle ?? "inproces",
                        StatusTranslation = t.StatusTranslation ?? "",
                        AgeRestriction = t.AgeRestriction
                    })
                    .OrderBy(x => Guid.NewGuid())
                    .Take(14)
                    .ToListAsync();

                _logger.LogInformation($"Returning {featuredTitles.Count} featured titles");
                return Ok(featuredTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching featured titles" });
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

                var popularPool = await _context.Titles
                    .AsNoTracking()
                    .Where(t => t.IsAvailable && t.TitleCategory != TitleCategory.Fanfic)
                    .Select(t => new
                    {
                        t.Id,
                        t.OriginalTitle,
                        t.EnglishTitle,
                        t.CoverImagePath,
                        t.Type,
                        t.TitleCategory,
                        t.ReleaseDate,
                        ChapterCount = t.Chapters.Count(),
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count(),
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => r.Value) : 0.0,
                        BookmarkCount = t.Bookmarks.Count(),
                        LastChapterDate = t.Chapters.Any()
                            ? (DateTime?)t.Chapters.Max(c => c.ReleaseDate)
                            : null,
                        LatestChapterNumber = t.Chapters.Any()
                            ? (double)t.Chapters
                                .OrderByDescending(c => c.VolumeNumber)
                                .ThenByDescending(c => c.ChapterNumber)
                                .Select(c => c.ChapterNumber)
                                .FirstOrDefault()
                            : 0.0,
                        PopularityScore =
                            (t.Chapters.Count() * 2.0) +
                            (t.Chapters.SelectMany(c => c.Views).Count() * 0.1) +
                            (t.Ratings.Any() ? t.Ratings.Average(r => r.Value) * 10 : 0) +
                            (t.Bookmarks.Count() * 5.0)
                    })
                    .OrderByDescending(x => x.PopularityScore)
                    .Take(60)
                    .ToListAsync();

                var result = popularPool
                    .OrderByDescending(x => x.LastChapterDate)
                    .Take(20)
                    .Select(item => new TitleListDto
                    {
                        Id = item.Id,
                        OriginalTitle = item.OriginalTitle ?? "Unknown Title",
                        EnglishTitle = item.EnglishTitle ?? item.OriginalTitle ?? "Unknown Title",
                        CoverImagePath = !string.IsNullOrEmpty(item.CoverImagePath) ? item.CoverImagePath : "/img/logo.png",
                        Type = item.Type,
                        TitleCategory = item.TitleCategory,
                        LatestChapter = item.LatestChapterNumber > 0 ? $"Ch. {(int)item.LatestChapterNumber}" : null,
                        LatestChapterNumber = item.LatestChapterNumber,
                        LastUpdated = item.LastChapterDate,
                        ChapterCount = item.ChapterCount,
                        AverageRating = item.AverageRating,
                        BookmarkCount = item.BookmarkCount,
                        ReleaseDate = item.ReleaseDate ?? "Unknown"
                    })
                    .ToList();

                _logger.LogInformation("Returning {Count} popular titles", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching popular titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching popular titles" });
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
                    .AsNoTracking()
                    .Where(t => t.IsAvailable && t.Chapters.Any())
                    .Select(t => new
                    {
                        t.Id,
                        t.OriginalTitle,
                        t.CoverImagePath,
                        t.Description,
                        LastChapterDate = t.Chapters
                            .OrderByDescending(c => c.ReleaseDate)
                            .Select(c => c.ReleaseDate)
                            .FirstOrDefault(),
                        LatestChapterNumber = t.Chapters
                            .OrderByDescending(c => c.ReleaseDate)
                            .Select(c => c.ChapterNumber)
                            .FirstOrDefault(),
                        LatestChapterName = t.Chapters
                            .OrderByDescending(c => c.ReleaseDate)
                            .Select(c => c.Name)
                            .FirstOrDefault(),
                        LatestChapterVolume = t.Chapters
                            .OrderByDescending(c => c.ReleaseDate)
                            .Select(c => c.VolumeNumber)
                            .FirstOrDefault(),
                        LatestChapterTeamName = t.Chapters
                            .OrderByDescending(c => c.ReleaseDate)
                            .Select(c => c.Team.Name)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(x => x.LastChapterDate)
                    .Take(10)
                    .ToListAsync();

                var result = recentUpdates.Select(item => new TitleUpdateDto
                {
                    Id = item.Id,
                    OriginalTitle = item.OriginalTitle ?? "Unknown Title",
                    CoverImagePath = !string.IsNullOrEmpty(item.CoverImagePath) ? item.CoverImagePath : "/img/logo.png",
                    Description = !string.IsNullOrEmpty(item.Description) && item.Description.Length > 200
                                    ? item.Description.Substring(0, 200) + "..."
                                    : item.Description ?? "No description available",
                    TeamName = item.LatestChapterTeamName ?? "Unknown Team",
                    TimeAgo = GetTimeAgo(item.LastChapterDate),
                    LatestChapter = $"Vol.{item.LatestChapterVolume} Ch.{item.LatestChapterNumber}" +
                                    (!string.IsNullOrEmpty(item.LatestChapterName) ? $": {item.LatestChapterName}" : ""),
                    LastUpdated = item.LastChapterDate
                }).ToList();

                _logger.LogInformation("Returning {Count} recent updates", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent updates: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching recent updates" });
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
                return StatusCode(500, new { message = "Error fetching title details" });
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
                    .AsNoTracking()
                    .AsSplitQuery()          // ← prevents cartesian explosion on multiple collections
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
                return StatusCode(500, new { message = "Error fetching title" });
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
                if (originalTitle?.Length > 255 || englishTitle?.Length > 255 || alternativeNames?.Length > 1000)
                    return BadRequest(new { message = "Title parameters exceed maximum allowed length." });

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
                return StatusCode(500, new { message = "Error checking similarity" });
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
                return StatusCode(500, new { message = "Error fetching bookmarks" });
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
                return StatusCode(500, new { message = "Error fetching trending titles" });
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
            if (query.Length > 100)
                return BadRequest(new { message = "Search query must not exceed 100 characters." });

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
                    Categories = t.Categories.OrderBy(c => c.Name).Select(c => c.Name).Take(3).ToList()
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
            if (query.Length > 100)
                return BadRequest(new { message = "Search query must not exceed 100 characters." });

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
                if (search?.Length > 100) return BadRequest(new { message = "Search query must not exceed 100 characters." });

                // Base query for filtering, sorting, and counting — no Includes needed here
                // because only title IDs are materialized from this query (.Select(t => t.Id)).
                // Full navigation properties are loaded in the second targeted query below.
                var query = _context.Titles
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

                // Load titles with lookup collections only.
                // Chapters are NOT included here — loading full chapter content (which EF pulls via Include)
                // for every chapter of every title on the page was the primary cause of the 30s timeout.
                // Chapter stats are fetched as a separate lightweight batch query below.
                var titles = await _context.Titles
                    .Where(t => titleIds.Contains(t.Id))
                    .AsSplitQuery()
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Formats)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Publishers)
                    .Include(t => t.Teams)
                    .ToListAsync();

                // Batch-fetch all stats for the page in 3 queries (eliminates N+1 — was up to 72 queries).

                var ratingStatsByTitle = await _context.Ratings
                    .Where(r => titleIds.Contains(r.TitleId))
                    .GroupBy(r => r.TitleId)
                    .Select(g => new { TitleId = g.Key, Average = g.Average(r => (double)r.Value), Count = g.Count() })
                    .ToDictionaryAsync(x => x.TitleId);

                var bookmarkCountByTitle = await _context.Bookmarks
                    .Where(b => titleIds.Contains(b.TitleId))
                    .GroupBy(b => b.TitleId)
                    .Select(g => new { TitleId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TitleId, x => x.Count);

                // Chapter count + latest chapter info — projected fields only, no Content column.
                var chapterStatsByTitle = await _context.Chapters
                    .Where(c => titleIds.Contains(c.TitleId))
                    .GroupBy(c => c.TitleId)
                    .Select(g => new {
                        TitleId = g.Key,
                        Count = g.Count(),
                        LatestChapterNumber = (int?)g.Max(c => (int?)c.ChapterNumber),
                        LatestReleaseDate = (DateTime?)g.Max(c => c.ReleaseDate)
                    })
                    .ToDictionaryAsync(x => x.TitleId);

                // Aggregate view counts per title in one query via Chapter navigation.
                // Avoids passing potentially thousands of chapter IDs as SQL parameters.
                var viewCountByTitle = await _context.ChapterViews
                    .Where(cv => titleIds.Contains(cv.Chapter.TitleId))
                    .GroupBy(cv => cv.Chapter.TitleId)
                    .Select(g => new { TitleId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TitleId, x => x.Count);

                var items = new List<TitleCatalogDto>();

                foreach (var title in titles)
                {
                    var ratingStats   = ratingStatsByTitle.TryGetValue(title.Id, out var rs) ? rs : null;
                    var bookmarkCount = bookmarkCountByTitle.TryGetValue(title.Id, out var bc) ? bc : 0;
                    var viewCount     = viewCountByTitle.TryGetValue(title.Id, out var vc) ? vc : 0;
                    var chapterStats  = chapterStatsByTitle.TryGetValue(title.Id, out var cs) ? cs : null;

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
                        LatestChapter = chapterStats?.LatestChapterNumber != null
                            ? $"Ch. {chapterStats.LatestChapterNumber}"
                            : null,
                        ChapterCount = chapterStats?.Count ?? 0,
                        ReleaseDate = title.ReleaseDate ?? "Unknown",
                        LastUpdated = chapterStats?.LatestReleaseDate,
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
                return StatusCode(500, new { message = "Error fetching catalog" });
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
                return StatusCode(500, new { message = "Error fetching filter options" });
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

        #region Chapter Management (Edit) Endpoints

        /// <summary>
        /// Get chapters for a title that the current user can edit.
        /// GET: api/Titles/{titleId}/chapters/manage
        /// Returns chapters + team dropdown scoped by title category:
        ///   Translation (1)  → team-based filtering
        ///   Original (2) / Fanfic (3) → title creator only
        ///   AITranslation (4) → admin only
        /// </summary>
        [HttpGet("{titleId:int}/chapters/manage")]
        [Authorize]
        public async Task<ActionResult> GetChaptersForManagement(int titleId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var title = await _context.Titles
                    .FirstOrDefaultAsync(t => t.Id == titleId);
                if (title == null)
                    return NotFound(new { message = "Title not found" });

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var category = title.TitleCategory;

                // ── Category gate ──────────────────────────────────────────────
                // AI/TL: admin-only editing
                if (category == TitleCategory.AITranslation && !isAdmin)
                    return StatusCode(403, new { message = "Only admins can edit AI-translated chapters." });

                // Original / Fanfic: only the title creator (or admin)
                if ((category == TitleCategory.Original || category == TitleCategory.Fanfic)
                    && !isAdmin && title.CreatedByUserId != user.Id)
                    return StatusCode(403, new { message = "Only the title author can edit chapters for this title." });

                // ── Load all chapters for this title ──────────────────────────
                var allChapters = await _context.Chapters
                    .Include(c => c.Team)
                    .Where(c => c.TitleId == titleId)
                    .OrderBy(c => c.VolumeNumber)
                    .ThenBy(c => c.ChapterNumber)
                    .ToListAsync();

                // ── Determine which chapters this user can see/edit ────────────
                List<Chapter> visibleChapters;
                if (isAdmin)
                {
                    visibleChapters = allChapters;
                }
                else if (category == TitleCategory.Original || category == TitleCategory.Fanfic)
                {
                    // Creator sees all chapters of their own title
                    visibleChapters = allChapters;
                }
                else
                {
                    // Translation: only chapters from teams the user can edit in
                    var editableTeamIds = await GetAuthorizedTeamIds(user.Id, "CanEditChapter");
                    visibleChapters = allChapters.Where(c =>
                        c.UpdatedByUserId == user.Id ||
                        (c.TeamId.HasValue && editableTeamIds.Contains(c.TeamId.Value))
                    ).ToList();
                }

                // ── Pending-edit map (scoped to visible chapters) ─────────────
                var visibleChapterIds = visibleChapters.Select(c => c.Id).ToHashSet();
                var pendingEdits = await _context.PendingChapters
                    .Where(pc => pc.TitleId == titleId &&
                                 pc.OriginalChapterId != null &&
                                 visibleChapterIds.Contains(pc.OriginalChapterId.Value))
                    .Select(pc => new { pc.OriginalChapterId, pc.Id })
                    .ToListAsync();
                var pendingEditMap = pendingEdits.ToDictionary(p => p.OriginalChapterId!.Value, p => p.Id);

                var chapterList = visibleChapters.Select(c =>
                {
                    pendingEditMap.TryGetValue(c.Id, out int pendingId);
                    return new
                    {
                        c.Id,
                        c.Name,
                        c.VolumeNumber,
                        c.ChapterNumber,
                        c.TitleId,
                        TeamId = c.TeamId,
                        TeamName = c.Team?.Name,
                        c.CreatedDate,
                        c.LastUpdatedAt,
                        CanEdit = true,
                        HasPendingEdit = pendingId != 0,
                        PendingEditId = pendingId != 0 ? (int?)pendingId : null
                    };
                }).ToList();

                // ── Team dropdown ──────────────────────────────────────────────
                // Translation: intersection of user's authorised teams ∩ teams with chapters here
                // Original / Fanfic: no team selector (creator edits directly)
                // AI/TL: admin sees all teams on this title
                var teamIdsOnTitle = allChapters
                    .Where(c => c.TeamId.HasValue)
                    .Select(c => c.TeamId!.Value)
                    .Distinct()
                    .ToHashSet();

                if (category == TitleCategory.Translation)
                {
                    if (isAdmin)
                    {
                        var adminTeams = await _context.Teams
                            .Where(t => teamIdsOnTitle.Contains(t.Id))
                            .Select(t => new { t.Id, t.Name })
                            .ToListAsync();

                        return Ok(new
                        {
                            TitleId = titleId,
                            TitleName = title.OriginalTitle,
                            TitleCategory = (int)category,
                            Chapters = chapterList,
                            UserTeams = adminTeams
                        });
                    }
                    else
                    {
                        var authorisedTeamsOnTitle = await _context.UserTeamRoles
                            .Include(utr => utr.Team)
                            .Include(utr => utr.UserTeamRolePermissions)
                                .ThenInclude(p => p.UserTeamPermission)
                            .Where(utr =>
                                utr.AppUserId == user.Id &&
                                teamIdsOnTitle.Contains(utr.TeamId) &&
                                (
                                    utr.Team.CreatorId == user.Id ||
                                    utr.Role == TeamRole.Admin ||
                                    (utr.Role == TeamRole.Member &&
                                     utr.UserTeamRolePermissions.Any(p =>
                                         p.UserTeamPermission.PermissionName == "CanEditChapter" ||
                                         p.UserTeamPermission.PermissionName == "CanAddChapter"))
                                ))
                            .Select(utr => new { utr.Team.Id, utr.Team.Name })
                            .Distinct()
                            .ToListAsync();

                        return Ok(new
                        {
                            TitleId = titleId,
                            TitleName = title.OriginalTitle,
                            TitleCategory = (int)category,
                            Chapters = chapterList,
                            UserTeams = authorisedTeamsOnTitle
                        });
                    }
                }
                else
                {
                    // Original / Fanfic / AITranslation — no team dropdown needed
                    return Ok(new
                    {
                        TitleId = titleId,
                        TitleName = title.OriginalTitle,
                        TitleCategory = (int)category,
                        Chapters = chapterList,
                        UserTeams = Array.Empty<object>()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters for management for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error retrieving chapters for management" });
            }
        }

        /// <summary>
        /// Get a single published chapter's content for editing.
        /// GET: api/Titles/{titleId}/chapters/{chapterId}/edit
        /// </summary>
        [HttpGet("{titleId:int}/chapters/{chapterId:int}/edit")]
        [Authorize]
        public async Task<ActionResult> GetChapterForEdit(int titleId, int chapterId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var chapter = await _context.Chapters
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .FirstOrDefaultAsync(c => c.Id == chapterId && c.TitleId == titleId);

                if (chapter == null)
                    return NotFound(new { message = "Chapter not found" });

                bool canEdit = await CanUserEditChapter(user.Id, chapter);
                if (!canEdit)
                    return StatusCode(403, new { message = "You do not have permission to edit this chapter." });

                // Check for an existing pending edit
                var pendingEdit = await _context.PendingChapters
                    .FirstOrDefaultAsync(pc => pc.OriginalChapterId == chapterId);

                return Ok(new
                {
                    chapter.Id,
                    chapter.Name,
                    chapter.VolumeNumber,
                    chapter.ChapterNumber,
                    chapter.TitleId,
                    TitleName = chapter.Title.OriginalTitle,
                    chapter.TeamId,
                    TeamName = chapter.Team?.Name,
                    chapter.Content,
                    chapter.CreatedDate,
                    chapter.LastUpdatedAt,
                    HasPendingEdit = pendingEdit != null,
                    PendingEdit = pendingEdit == null ? null : new
                    {
                        pendingEdit.Id,
                        pendingEdit.Name,
                        pendingEdit.VolumeNumber,
                        pendingEdit.ChapterNumber,
                        pendingEdit.Content,
                        pendingEdit.TeamId,
                        pendingEdit.CreatedDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapter {ChapterId} for edit", chapterId);
                return StatusCode(500, new { message = "Error retrieving chapter for editing" });
            }
        }

        /// <summary>
        /// Submit an edit to a published chapter.
        /// PUT: api/Titles/{titleId}/chapters/{chapterId}
        /// Creates a PendingChapter with OriginalChapterId set, or directly updates if trusted.
        /// </summary>
        [HttpPut("{titleId:int}/chapters/{chapterId:int}")]
        [Authorize]
        public async Task<ActionResult> UpdateChapter(int titleId, int chapterId, [FromBody] UpdateChapterRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var chapter = await _context.Chapters
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .FirstOrDefaultAsync(c => c.Id == chapterId && c.TitleId == titleId);

                if (chapter == null)
                    return NotFound(new { message = "Chapter not found" });

                bool canEdit = await CanUserEditChapter(user.Id, chapter);
                if (!canEdit)
                    return StatusCode(403, new { message = "You do not have permission to edit this chapter." });

                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { message = "Chapter content cannot be empty." });

                // Validate team if changed
                int resolvedTeamId = request.TeamId > 0 ? request.TeamId : (chapter.TeamId ?? 0);
                if (resolvedTeamId <= 0)
                    return BadRequest(new { message = "A valid team must be selected." });

                // Remove any existing pending edit for this chapter to avoid duplicates
                var existingPending = await _context.PendingChapters
                    .FirstOrDefaultAsync(pc => pc.OriginalChapterId == chapterId);
                if (existingPending != null)
                    _context.PendingChapters.Remove(existingPending);

                bool isTrusted = await _trustService.IsTrustedAsync(user.Id, TrustActionType.AddChapter);

                if (isTrusted)
                {
                    // Directly update the chapter
                    string oldContent = chapter.Content;
                    string oldName = chapter.Name;

                    chapter.Name = request.Name ?? chapter.Name;
                    chapter.VolumeNumber = request.VolumeNumber > 0 ? request.VolumeNumber : chapter.VolumeNumber;
                    chapter.ChapterNumber = request.ChapterNumber > 0 ? request.ChapterNumber : chapter.ChapterNumber;
                    chapter.TeamId = resolvedTeamId;
                    chapter.Content = request.Content;
                    chapter.LastUpdatedAt = DateTime.UtcNow;
                    chapter.UpdatedByUserId = user.Id;

                    var changeLog = new TitleChangeLog
                    {
                        TitleId = titleId,
                        UpdatedByUserId = user.Id,
                        ReviewedByUserId = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        ReviewedAt = DateTime.UtcNow,
                        ChangeType = "Edit Chapter",
                        OldValue = $"Ch.{chapter.ChapterNumber} - {oldName}",
                        NewValue = $"Ch.{chapter.ChapterNumber} - {chapter.Name}",
                        AdminComment = "Auto-approved (trusted user)",
                        Status = ChangeLogStatus.AutoApproved,
                    };
                    _context.TitleChangeLogs.Add(changeLog);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Chapter {ChapterId} directly updated (trusted) by {User}", chapterId, user.UserName);
                    return Ok(new { message = "Chapter updated successfully!", autoApproved = true, chapterId });
                }
                else
                {
                    // Create a pending edit
                    var pendingChapter = new PendingChapter
                    {
                        Name = request.Name ?? chapter.Name,
                        VolumeNumber = request.VolumeNumber > 0 ? request.VolumeNumber : chapter.VolumeNumber,
                        ChapterNumber = request.ChapterNumber > 0 ? request.ChapterNumber : chapter.ChapterNumber,
                        TitleId = titleId,
                        TeamId = resolvedTeamId,
                        Content = request.Content,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedByUserId = user.Id,
                        OriginalChapterId = chapterId,
                        CharacterCount = request.Content?.Length ?? 0
                    };
                    _context.PendingChapters.Add(pendingChapter);

                    // Log to change history as Pending
                    var changeLog = new TitleChangeLog
                    {
                        TitleId = titleId,
                        UpdatedByUserId = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        ChangeType = "Edit Chapter",
                        OldValue = $"Ch.{chapter.ChapterNumber} - {chapter.Name}",
                        NewValue = $"Ch.{request.ChapterNumber} - {request.Name ?? chapter.Name}",
                        Status = ChangeLogStatus.Pending,
                    };
                    _context.TitleChangeLogs.Add(changeLog);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Chapter edit pending review for chapter {ChapterId} by {User}", chapterId, user.UserName);
                    return Ok(new { message = "Chapter edit submitted for review.", autoApproved = false, pendingId = pendingChapter.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chapter {ChapterId}", chapterId);
                return StatusCode(500, new { message = "Error updating chapter" });
            }
        }

        /// <summary>
        /// Permanently delete a published chapter and any associated pending edits.
        /// DELETE: api/Titles/{titleId}/chapters/{chapterId}
        /// Requires CanDeleteChapter permission within the chapter's team (or admin).
        /// </summary>
        [HttpDelete("{titleId:int}/chapters/{chapterId:int}")]
        [Authorize]
        public async Task<ActionResult> DeleteChapter(int titleId, int chapterId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var chapter = await _context.Chapters
                    .Include(c => c.Title)
                    .Include(c => c.Team)
                    .FirstOrDefaultAsync(c => c.Id == chapterId && c.TitleId == titleId);

                if (chapter == null)
                    return NotFound(new { message = "Chapter not found." });

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var category = chapter.Title?.TitleCategory ?? TitleCategory.Translation;

                // ── Category-aware delete permission ─────────────────────────────
                bool canDelete = false;

                if (isAdmin)
                {
                    canDelete = true;
                }
                else if (category == TitleCategory.AITranslation)
                {
                    // AI/TL: admin-only
                    canDelete = false;
                }
                else if (category == TitleCategory.Original || category == TitleCategory.Fanfic)
                {
                    // Own work: only the title creator
                    canDelete = chapter.Title?.CreatedByUserId == user.Id;
                }
                else
                {
                    // Translation: must have CanDeleteChapter in the chapter's team
                    canDelete = await _context.UserTeamRoles
                        .Where(utr => utr.AppUserId == user.Id && utr.TeamId == chapter.TeamId)
                        .Where(utr =>
                            utr.Team.CreatorId == user.Id ||
                            utr.Role == TeamRole.Admin ||
                            (utr.Role == TeamRole.Member &&
                             utr.UserTeamRolePermissions.Any(p =>
                                 p.UserTeamPermission.PermissionName == "CanDeleteChapter")))
                        .AnyAsync();
                }

                if (!canDelete)
                    return StatusCode(403, new { message = "You do not have permission to delete this chapter." });

                // Remove any pending edits that reference this chapter
                var pendingEdits = await _context.PendingChapters
                    .Where(pc => pc.OriginalChapterId == chapterId)
                    .ToListAsync();
                if (pendingEdits.Any())
                    _context.PendingChapters.RemoveRange(pendingEdits);

                _context.Chapters.Remove(chapter);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Chapter {ChapterId} (title {TitleId}) deleted by user {UserId}",
                    chapterId, titleId, user.Id);

                return Ok(new { message = "Chapter deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chapter {ChapterId}", chapterId);
                return StatusCode(500, new { message = "Error deleting chapter." });
            }
        }

        #endregion
    }
}

// DTO for chapter update request
public record UpdateChapterRequest(
    string? Name,
    int VolumeNumber,
    int ChapterNumber,
    int TeamId,
    string Content
);