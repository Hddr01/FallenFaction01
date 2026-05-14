using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.AI;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/translation-requests")]
    [Authorize]
    public class TranslationRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TranslationRequestsController> _logger;

        public TranslationRequestsController(
            ApplicationDbContext context,
            ILogger<TranslationRequestsController> logger)
        {
            _context = context;
            _logger  = logger;
        }

        // ── GET /api/translation-requests  (public — all approved+) ──────────
        /// <summary>
        /// Public feed of all Approved/Released/Rejected requests with vote counts.
        /// Used for the community voting page.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TranslationRequestDto>>> GetAll(
            [FromQuery] string? status  = null,   // filter by status name
            [FromQuery] string  orderBy = "votes", // "votes" | "newest"
            [FromQuery] int     page    = 1,
            [FromQuery] int     pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.TranslationRequests
                .Include(r => r.RequestedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status)
                && Enum.TryParse<TranslationRequestStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }

            query = orderBy == "votes"
                ? query.OrderByDescending(r => r.VoteCount).ThenByDescending(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var rows  = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Resolve which ones the current user has voted on
            var userVotedIds = userId == null
                ? new HashSet<int>()
                : (await _context.TranslationRequestVotes
                    .Where(v => v.UserId == userId)
                    .Select(v => v.RequestId)
                    .ToListAsync())
                  .ToHashSet();

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(rows.Select(r => MapToDto(r, userVotedIds.Contains(r.Id))));
        }

        // ── GET /api/translation-requests/my  (own requests) ─────────────────
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<TranslationRequestDto>>> GetMine(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.TranslationRequests
                .Include(r => r.RequestedByUser)
                .Where(r => r.RequestedByUserId == userId);

            if (!string.IsNullOrEmpty(status)
                && Enum.TryParse<TranslationRequestStatus>(status, true, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);

            query = query.OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var rows  = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var userVotedIds = (await _context.TranslationRequestVotes
                    .Where(v => v.UserId == userId)
                    .Select(v => v.RequestId)
                    .ToListAsync())
                .ToHashSet();

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(rows.Select(r => MapToDto(r, userVotedIds.Contains(r.Id))));
        }

        // ── POST /api/translation-requests  (submit new request) ─────────────
        [HttpPost]
        public async Task<ActionResult<TranslationRequestDto>> Create(
            [FromBody] CreateTranslationRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Prevent duplicate submissions (same URL already Pending or Approved)
            var duplicate = await _context.TranslationRequests.AnyAsync(r =>
                r.SourceUrl == dto.SourceUrl
                && (r.Status == TranslationRequestStatus.Pending
                    || r.Status == TranslationRequestStatus.Approved
                    || r.Status == TranslationRequestStatus.PreProcessing));

            if (duplicate)
                return Conflict("A request for this URL is already in progress.");

            var request = new TranslationRequest
            {
                RequestedByUserId    = userId!,
                SourceUrl            = dto.SourceUrl,
                ProposedTitle        = dto.ProposedTitle,
                OriginalLanguageTitle = dto.OriginalLanguageTitle,
                Description          = dto.Description,
                Genres               = dto.Genres,
                Tags                 = dto.Tags,
                CoverImageUrl        = dto.CoverImageUrl,
                EstimatedChapterCount = dto.EstimatedChapterCount,
                Status               = TranslationRequestStatus.Pending,
                CreatedAt            = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow
            };

            _context.TranslationRequests.Add(request);
            await _context.SaveChangesAsync();

            var created = await _context.TranslationRequests
                .Include(r => r.RequestedByUser)
                .FirstAsync(r => r.Id == request.Id);

            return CreatedAtAction(nameof(GetAll), new { id = request.Id }, MapToDto(created, false));
        }

        // ── POST /api/translation-requests/{id}/vote ──────────────────────────
        [HttpPost("{id}/vote")]
        public async Task<IActionResult> Vote(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user   = await _context.Users.FindAsync(userId);

            if (user == null) return Unauthorized();

            // Vote eligibility: Level 2+
            if (!user.CanVote)
                return StatusCode(403, "You need to reach Level 2 to vote.");

            var request = await _context.TranslationRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (request.Status != TranslationRequestStatus.Approved)
                return BadRequest("Voting is only open on Approved requests.");

            var existing = await _context.TranslationRequestVotes
                .FirstOrDefaultAsync(v => v.RequestId == id && v.UserId == userId);

            if (existing != null)
            {
                // Toggle off — unvote
                _context.TranslationRequestVotes.Remove(existing);
                request.VoteCount = Math.Max(0, request.VoteCount - 1);
                await _context.SaveChangesAsync();
                return Ok(new { voted = false, voteCount = request.VoteCount });
            }

            // Cast vote
            _context.TranslationRequestVotes.Add(new TranslationRequestVote
            {
                RequestId = id,
                UserId    = userId!,
                VotedAt   = DateTime.UtcNow
            });
            request.VoteCount++;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { voted = true, voteCount = request.VoteCount });
        }

        // ════════════════════════════════════════════════════════════════════
        // ADMIN ENDPOINTS
        // ════════════════════════════════════════════════════════════════════

        // ── GET /api/translation-requests/admin/queue  [Admin] ────────────────
        [HttpGet("admin/queue")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<TranslationRequestDto>>> AdminQueue(
            [FromQuery] string? status   = null,
            [FromQuery] string  orderBy  = "newest",
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 50)
        {
            var query = _context.TranslationRequests
                .Include(r => r.RequestedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status)
                && Enum.TryParse<TranslationRequestStatus>(status, true, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);

            query = orderBy == "votes"
                ? query.OrderByDescending(r => r.VoteCount).ThenByDescending(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var rows  = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(rows.Select(r => MapToDto(r, false)));
        }

        // ── POST /api/translation-requests/admin/review  [Admin] ─────────────
        [HttpPost("admin/review")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReview([FromBody] AdminReviewRequestDto dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var request = await _context.TranslationRequests.FindAsync(dto.RequestId);
            if (request == null) return NotFound();

            request.ReviewedByUserId = adminId;
            request.ReviewedAt       = DateTime.UtcNow;
            request.UpdatedAt        = DateTime.UtcNow;

            switch (dto.Action.ToLower())
            {
                case "approve":
                    request.Status = TranslationRequestStatus.Approved;
                    break;

                case "reject":
                    if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                        return BadRequest("Rejection reason is required.");
                    request.Status          = TranslationRequestStatus.Rejected;
                    request.RejectionReason = dto.RejectionReason;
                    break;

                case "preprocessing":
                    request.Status = TranslationRequestStatus.PreProcessing;
                    break;

                default:
                    return BadRequest($"Unknown action '{dto.Action}'.");
            }

            if (dto.AdminNotes != null)
                request.AdminNotes = dto.AdminNotes;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Request {dto.RequestId} → {request.Status}." });
        }

        // ── POST /api/translation-requests/admin/release  [Admin] ────────────
        /// <summary>
        /// Marks a request as Released and links it to the Title that was created.
        /// The admin must have already created the Title under the AI/TL team.
        /// </summary>
        [HttpPost("admin/release")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminRelease([FromBody] AdminReleaseRequestDto dto)
        {
            var request = await _context.TranslationRequests.FindAsync(dto.RequestId);
            if (request == null)
                return NotFound(new { message = $"Translation request #{dto.RequestId} not found." });

            if (request.Status != TranslationRequestStatus.PreProcessing)
                return BadRequest(new { message = $"Request must be in PreProcessing status to release. Current status: {request.Status}." });

            var title = await _context.Titles
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == dto.TitleId);

            if (title == null)
                return NotFound(new { message = $"Title with ID {dto.TitleId} not found. Make sure you created the title first and entered the correct ID." });

            if ((int)title.TitleCategory != 4)
                return BadRequest(new { message = $"Title #{dto.TitleId} is not an AI Translation title (category is '{title.TitleCategory}'). Only AI Translation titles can be released via requests." });

            var aiTeam = title.Teams.FirstOrDefault(t => t.IsSystemTeam);

            request.Status          = TranslationRequestStatus.Released;
            request.ReleasedTitleId = dto.TitleId;
            request.ReleasedTeamId  = aiTeam?.Id;
            request.ReleasedAt      = DateTime.UtcNow;
            request.UpdatedAt       = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message    = $"Request #{dto.RequestId} released as \"{title.EnglishTitle ?? title.OriginalTitle}\" (Title ID {dto.TitleId}).",
                titleId    = dto.TitleId,
                titleName  = title.EnglishTitle ?? title.OriginalTitle,
                requestId  = dto.RequestId
            });
        }

        // ── Mapper ────────────────────────────────────────────────────────────

        // ── GET /api/translation-requests/admin/search-titles  [Admin] ─────────
        /// <summary>
        /// Search AI Translation titles by name — used by the release modal dropdown.
        /// </summary>
        [HttpGet("admin/search-titles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchAiTitles([FromQuery] string q = "")
        {
            var query = _context.Titles
                .Where(t => t.TitleCategory == TitleCategory.AITranslation && t.IsAvailable);

            if (q.Length > 100)
                return BadRequest(new { message = "Search query must not exceed 100 characters." });
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(t =>
                    t.EnglishTitle.Contains(q) || t.OriginalTitle.Contains(q));

            var results = await query
                .OrderBy(t => t.EnglishTitle)
                .Take(20)
                .Select(t => new { id = t.Id, name = t.EnglishTitle ?? t.OriginalTitle })
                .ToListAsync();

            return Ok(results);
        }

        private static TranslationRequestDto MapToDto(TranslationRequest r, bool hasUserVoted) =>
            new()
            {
                Id                    = r.Id,
                RequestedByUserId     = r.RequestedByUserId,
                RequestedByUserName   = r.RequestedByUser?.UserName ?? "Unknown",
                SourceUrl             = r.SourceUrl,
                ProposedTitle         = r.ProposedTitle,
                OriginalLanguageTitle = r.OriginalLanguageTitle,
                Description           = r.Description,
                Genres                = r.Genres,
                Tags                  = r.Tags,
                CoverImageUrl         = r.CoverImageUrl,
                EstimatedChapterCount = r.EstimatedChapterCount,
                VoteCount             = r.VoteCount,
                HasUserVoted          = hasUserVoted,
                Status                = r.Status.ToString(),
                RejectionReason       = r.RejectionReason,
                ReleasedTitleId       = r.ReleasedTitleId,
                CreatedAt             = r.CreatedAt,
                ReviewedAt            = r.ReviewedAt,
                ReleasedAt            = r.ReleasedAt
            };
    }
}
