using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/title-join-requests")]
    [Authorize]
    public class TitleJoinRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TitleJoinRequestsController> _logger;

        private static readonly TimeSpan ActiveThreshold = TimeSpan.FromDays(180);

        public TitleJoinRequestsController(ApplicationDbContext context, ILogger<TitleJoinRequestsController> logger)
        {
            _context = context;
            _logger  = logger;
        }

        // ── POST /api/title-join-requests ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitJoinRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var title = await _context.Titles
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == dto.TitleId && t.IsAvailable);

            if (title == null)
                return NotFound(new { message = "Title not found." });

            if (title.TitleCategory != TitleCategory.Translation && title.TitleCategory != TitleCategory.AITranslation)
                return BadRequest(new { message = "Join requests are only for Translation titles." });

            // Verify user has admin role in the requesting team
            var teamMembership = await _context.UserTeamRoles
                .Include(r => r.Team)
                .Include(r => r.UserTeamRolePermissions)
                    .ThenInclude(p => p.UserTeamPermission)
                .FirstOrDefaultAsync(r =>
                    r.AppUserId == userId &&
                    r.TeamId == dto.RequestingTeamId &&
                    (r.Team.CreatorId == userId ||
                     r.Role == TeamRole.Admin ||
                     r.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanManageTitle")));

            if (teamMembership == null)
                return StatusCode(403, new { message = "You must be an admin or manager of the requesting team." });

            if (teamMembership.Team.IsSystemTeam)
                return BadRequest(new { message = "The AI/TL system team is managed by admins only." });

            if (title.Teams.Any(t => t.Id == dto.RequestingTeamId))
                return Conflict(new { message = "Your team is already translating this title." });

            var existingPending = await _context.TitleTeamJoinRequests
                .AnyAsync(r => r.TitleId == dto.TitleId &&
                               r.RequestingTeamId == dto.RequestingTeamId &&
                               r.Status == JoinRequestStatus.Pending);
            if (existingPending)
                return Conflict(new { message = "Your team already has a pending request for this title." });

            // Check human teams for activity — ghost (system) teams are ignored
            var humanTeams = title.Teams.Where(t => !t.IsSystemTeam).ToList();

            foreach (var team in humanTeams)
            {
                var lastChapterDate = await _context.Chapters
                    .Where(c => c.TitleId == dto.TitleId && c.TeamId == team.Id)
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => (DateTime?)c.CreatedDate)
                    .FirstOrDefaultAsync();

                if (lastChapterDate.HasValue && DateTime.UtcNow - lastChapterDate.Value < ActiveThreshold)
                {
                    var autoReject = new TitleTeamJoinRequest
                    {
                        TitleId            = dto.TitleId,
                        RequestingTeamId   = dto.RequestingTeamId,
                        RequestedByUserId  = userId!,
                        Message            = dto.Message,
                        Status             = JoinRequestStatus.AutoRejected,
                        AutoRejectedReason = $"{team.Name} is actively translating this title (last update: {lastChapterDate.Value:MMMM d, yyyy}).",
                        CreatedAt          = DateTime.UtcNow,
                        ReviewedAt         = DateTime.UtcNow
                    };
                    _context.TitleTeamJoinRequests.Add(autoReject);
                    await _context.SaveChangesAsync();

                    return StatusCode(409, new
                    {
                        message      = $"Request auto-rejected: {team.Name} is actively translating this title (last chapter: {lastChapterDate.Value:MMMM d, yyyy}).",
                        autoRejected = true,
                        reason       = autoReject.AutoRejectedReason
                    });
                }
            }

            var request = new TitleTeamJoinRequest
            {
                TitleId           = dto.TitleId,
                RequestingTeamId  = dto.RequestingTeamId,
                RequestedByUserId = userId!,
                Message           = dto.Message,
                Status            = JoinRequestStatus.Pending,
                CreatedAt         = DateTime.UtcNow
            };
            _context.TitleTeamJoinRequests.Add(request);
            await _context.SaveChangesAsync();

            await NotifyReviewers(request, title, teamMembership.Team.Name);

            return Ok(new { message = "Request submitted. It will be reviewed by the team or a site admin.", requestId = request.Id });
        }

        // ── GET /api/title-join-requests/my ──────────────────────────────────────
        [HttpGet("my")]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var myTeamIds = await _context.UserTeamRoles
                .Where(r => r.AppUserId == userId)
                .Select(r => r.TeamId)
                .ToListAsync();

            var requests = await _context.TitleTeamJoinRequests
                .Include(r => r.Title)
                .Include(r => r.RequestingTeam)
                .Where(r => myTeamIds.Contains(r.RequestingTeamId))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(requests.Select(MapToDto));
        }

        // ── GET /api/title-join-requests/for-title/{titleId} ─────────────────────
        [HttpGet("for-title/{titleId:int}")]
        public async Task<IActionResult> GetForTitle(int titleId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !await CanManageTitle(userId!, titleId)) return Forbid();

            var requests = await _context.TitleTeamJoinRequests
                .Include(r => r.RequestingTeam)
                .Include(r => r.RequestedByUser)
                .Where(r => r.TitleId == titleId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(requests.Select(MapToDto));
        }

        // ── GET /api/title-join-requests/admin ───────────────────────────────────
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminQueue(
            [FromQuery] string? status   = null,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 50)
        {
            var query = _context.TitleTeamJoinRequests
                .Include(r => r.Title)
                .Include(r => r.RequestingTeam)
                .Include(r => r.RequestedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<JoinRequestStatus>(status, true, out var s))
                query = query.Where(r => r.Status == s);

            var total = await query.CountAsync();
            var rows  = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(rows.Select(MapToDto));
        }

        // ── POST /api/title-join-requests/{id}/approve ────────────────────────────
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = await _context.TitleTeamJoinRequests
                .Include(r => r.RequestingTeam)
                .Include(r => r.Title)
                    .ThenInclude(t => t!.Teams)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();
            if (request.Status != JoinRequestStatus.Pending)
                return BadRequest(new { message = "This request is not pending." });

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !await CanManageTitle(userId!, request.TitleId))
                return Forbid();

            // Add team to title
            var team = await _context.Teams.FindAsync(request.RequestingTeamId);
            if (team != null && !request.Title!.Teams.Any(t => t.Id == team.Id))
                request.Title.Teams.Add(team);

            // Grant permissions using correct composite-key model (AppUserId + TeamId + PermissionId)
            await EnsureTeamPermissions(request.RequestingTeamId);

            request.Status           = JoinRequestStatus.Approved;
            request.ReviewedByUserId = userId;
            request.ReviewedAt       = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Use cast to int for new enum values — add them to Notification.cs per PATCH file
            await SendNotification(
                request.RequestedByUserId,
                (NotificationType)32, // TitleJoinApproved
                "Translation Request Approved",
                $"Your request to translate \"{request.Title!.EnglishTitle ?? request.Title.OriginalTitle}\" with {request.RequestingTeam!.Name} has been approved.",
                $"/{request.TitleId}");

            return Ok(new { message = "Request approved. Team added to title." });
        }

        // ── POST /api/title-join-requests/{id}/reject ─────────────────────────────
        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectJoinRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = await _context.TitleTeamJoinRequests
                .Include(r => r.RequestingTeam)
                .Include(r => r.Title)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();
            if (request.Status != JoinRequestStatus.Pending)
                return BadRequest(new { message = "This request is not pending." });

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !await CanManageTitle(userId!, request.TitleId))
                return Forbid();

            request.Status           = isAdmin ? JoinRequestStatus.RejectedByAdmin : JoinRequestStatus.RejectedByTeam;
            request.RejectionReason  = dto.Reason;
            request.ReviewedByUserId = userId;
            request.ReviewedAt       = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await SendNotification(
                request.RequestedByUserId,
                (NotificationType)33, // TitleJoinRejected
                "Translation Request Rejected",
                $"Your request to translate \"{request.Title!.EnglishTitle ?? request.Title.OriginalTitle}\" was rejected: {dto.Reason}",
                null);

            return Ok(new { message = "Request rejected." });
        }

        // ── GET /api/title-join-requests/check/{titleId} ─────────────────────────
        [HttpGet("check/{titleId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckTitle(int titleId)
        {
            var title = await _context.Titles
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == titleId && t.IsAvailable);

            if (title == null) return NotFound();

            var humanTeams = title.Teams.Where(t => !t.IsSystemTeam).ToList();
            var teamActivities = new List<object>();
            bool hasActiveTeam = false;

            foreach (var team in humanTeams)
            {
                var last = await _context.Chapters
                    .Where(c => c.TitleId == titleId && c.TeamId == team.Id)
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => (DateTime?)c.CreatedDate)
                    .FirstOrDefaultAsync();

                var isActive = last.HasValue && DateTime.UtcNow - last.Value < ActiveThreshold;
                if (isActive) hasActiveTeam = true;

                teamActivities.Add(new { teamId = team.Id, teamName = team.Name, lastChapter = last, isActive });
            }

            return Ok(new
            {
                titleId,
                titleCategory     = (int)title.TitleCategory,
                translationStatus = title.StatusTranslation,
                hasHumanTeam      = humanTeams.Any(),
                hasActiveTeam,
                reasonRequired    = hasActiveTeam,
                teams             = teamActivities
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private async Task<bool> CanManageTitle(string userId, int titleId)
        {
            var titleTeamIds = await _context.Titles
                .Where(t => t.Id == titleId)
                .SelectMany(t => t.Teams.Select(team => team.Id))
                .ToListAsync();

            return await _context.UserTeamRoles
                .AnyAsync(r =>
                    r.AppUserId == userId &&
                    titleTeamIds.Contains(r.TeamId) &&
                    (r.Team.CreatorId == userId ||
                     r.Role == TeamRole.Admin ||
                     r.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanManageTitle")));
        }

        /// <summary>
        /// Grants translation permissions to all Admin members of the newly-joined team.
        /// Uses the real composite key: AppUserId + TeamId + PermissionId.
        /// </summary>
        private async Task EnsureTeamPermissions(int teamId)
        {
            var permNames = new[] { "CanAddChapter", "CanEditChapter", "CanAddTitle", "CanEditTitle", "CanManageTitle" };
            var perms = await _context.UserTeamPermissions
                .Where(p => permNames.Contains(p.PermissionName))
                .ToDictionaryAsync(p => p.PermissionName, p => p.Id);

            if (!perms.Any()) return;

            var teamAdmins = await _context.UserTeamRoles
                .Where(r => r.TeamId == teamId &&
                            (r.Role == TeamRole.Admin || r.Team.CreatorId == r.AppUserId))
                .ToListAsync();

            foreach (var role in teamAdmins)
            {
                var existingPermIds = await _context.UserTeamRolePermissions
                    .Where(p => p.AppUserId == role.AppUserId && p.TeamId == role.TeamId)
                    .Select(p => p.PermissionId)
                    .ToListAsync();

                foreach (var (_, permId) in perms)
                {
                    if (!existingPermIds.Contains(permId))
                    {
                        _context.UserTeamRolePermissions.Add(new UserTeamRolePermission
                        {
                            AppUserId    = role.AppUserId,
                            TeamId       = role.TeamId,
                            PermissionId = permId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task NotifyReviewers(TitleTeamJoinRequest req, Title title, string requestingTeamName)
        {
            var titleName  = title.EnglishTitle ?? title.OriginalTitle;
            var notifType  = (NotificationType)31; // TitleJoinRequest — see PATCH file

            var humanTeamIds = title.Teams.Where(t => !t.IsSystemTeam).Select(t => t.Id).ToList();
            if (humanTeamIds.Any())
            {
                var managerIds = await _context.UserTeamRoles
                    .Where(r => humanTeamIds.Contains(r.TeamId) &&
                                (r.Role == TeamRole.Admin ||
                                 r.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == "CanManageTitle")))
                    .Select(r => r.AppUserId)
                    .Distinct()
                    .ToListAsync();

                foreach (var mid in managerIds)
                    await SendNotification(mid, notifType,
                        "New Translation Join Request",
                        $"{requestingTeamName} wants to co-translate \"{titleName}\".",
                        "/admin/title-join-requests");
            }

            var adminRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin"))?.Id;
            if (adminRoleId != null)
            {
                var adminIds = await _context.UserRoles
                    .Where(r => r.RoleId == adminRoleId)
                    .Select(r => r.UserId)
                    .ToListAsync();

                foreach (var aid in adminIds)
                    await SendNotification(aid, notifType,
                        "New Translation Join Request",
                        $"{requestingTeamName} wants to co-translate \"{titleName}\".",
                        "/admin/title-join-requests");
            }
        }

        private async Task SendNotification(string userId, NotificationType type, string title, string message, string? linkUrl)
        {
            _context.Notifications.Add(new Notification
            {
                UserId    = userId,
                Type      = type,
                Title     = title,
                Message   = message,
                LinkUrl   = linkUrl,
                IsGlobal  = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private static JoinRequestDto MapToDto(TitleTeamJoinRequest r) => new()
        {
            Id                   = r.Id,
            TitleId              = r.TitleId,
            TitleName            = r.Title?.EnglishTitle ?? r.Title?.OriginalTitle ?? "",
            RequestingTeamId     = r.RequestingTeamId,
            RequestingTeamName   = r.RequestingTeam?.Name ?? "",
            RequestingTeamAvatar = r.RequestingTeam?.AvatarImagePath,
            RequestedByUserId    = r.RequestedByUserId,
            RequestedByUserName  = r.RequestedByUser?.UserName ?? "",
            Message              = r.Message,
            Status               = r.Status.ToString(),
            AutoRejectedReason   = r.AutoRejectedReason,
            RejectionReason      = r.RejectionReason,
            CreatedAt            = r.CreatedAt,
            ReviewedAt           = r.ReviewedAt
        };
    }

    public record SubmitJoinRequestDto(int TitleId, int RequestingTeamId, string? Message);
    public record RejectJoinRequestDto(string Reason);

    public class JoinRequestDto
    {
        public int Id { get; set; }
        public int TitleId { get; set; }
        public string TitleName { get; set; } = "";
        public int RequestingTeamId { get; set; }
        public string RequestingTeamName { get; set; } = "";
        public string? RequestingTeamAvatar { get; set; }
        public string RequestedByUserId { get; set; } = "";
        public string RequestedByUserName { get; set; } = "";
        public string? Message { get; set; }
        public string Status { get; set; } = "";
        public string? AutoRejectedReason { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
