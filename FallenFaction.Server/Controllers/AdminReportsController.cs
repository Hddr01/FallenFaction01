using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Moderator")]
    public class AdminReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AdminReportsController> _logger;

        public AdminReportsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<AdminReportsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all reports with filtering and pagination.
        /// GET: api/AdminReports?status=Pending&targetType=Comment&page=1&pageSize=20
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ReportsPagedResponse>> GetReports(
            [FromQuery] ReportStatus? status = null,
            [FromQuery] ReportTargetType? targetType = null,
            [FromQuery] ReportReason? reason = null,
            [FromQuery] string? searchQuery = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Reports
                    .Include(r => r.ReporterUser)
                    .Include(r => r.TargetComment)
                    .Include(r => r.TargetTitle)
                    .Include(r => r.TargetChapter)
                    .Include(r => r.TargetUser)
                    .Include(r => r.ReviewedByUser)
                    .AsQueryable();

                // Filters
                if (status.HasValue)
                    query = query.Where(r => r.Status == status.Value);

                if (targetType.HasValue)
                    query = query.Where(r => r.TargetType == targetType.Value);

                if (reason.HasValue)
                    query = query.Where(r => r.Reason == reason.Value);

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var search = searchQuery.ToLower();
                    query = query.Where(r =>
                        (r.Description != null && r.Description.ToLower().Contains(search)) ||
                        (r.ReporterUser != null && r.ReporterUser.UserName != null && r.ReporterUser.UserName.ToLower().Contains(search)) ||
                        (r.AdminNote != null && r.AdminNote.ToLower().Contains(search)));
                }

                var totalCount = await query.CountAsync();

                var reports = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReportDto
                    {
                        Id = r.Id,
                        ReporterUserId = r.ReporterUserId,
                        ReporterUserName = r.ReporterUser != null ? r.ReporterUser.UserName : null,
                        ReporterAvatar = r.ReporterUser != null ? r.ReporterUser.ProfilePicturePath : null,
                        TargetType = r.TargetType,
                        TargetCommentId = r.TargetCommentId,
                        TargetTitleId = r.TargetTitleId,
                        TargetChapterId = r.TargetChapterId,
                        TargetUserId = r.TargetUserId,
                        TargetPreview = r.TargetType == ReportTargetType.Comment && r.TargetComment != null
                            ? (r.TargetComment.Content.Length > 120 ? r.TargetComment.Content.Substring(0, 120) + "..." : r.TargetComment.Content)
                            : r.TargetType == ReportTargetType.Title && r.TargetTitle != null
                                ? r.TargetTitle.EnglishTitle
                                : r.TargetType == ReportTargetType.Chapter && r.TargetChapter != null
                                    ? r.TargetChapter.Name
                                    : null,
                        TargetUserName = r.TargetType == ReportTargetType.User && r.TargetUser != null
                            ? r.TargetUser.UserName : null,
                        Reason = r.Reason,
                        Description = r.Description,
                        Status = r.Status,
                        ReviewedByUserName = r.ReviewedByUser != null ? r.ReviewedByUser.UserName : null,
                        AdminNote = r.AdminNote,
                        CreatedAt = r.CreatedAt,
                        ReviewedAt = r.ReviewedAt
                    })
                    .ToListAsync();

                return Ok(new ReportsPagedResponse
                {
                    Reports = reports,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reports");
                return StatusCode(500, "An error occurred.");
            }
        }

        /// <summary>
        /// Get report counts by status (for dashboard badges).
        /// GET: api/AdminReports/counts
        /// </summary>
        [HttpGet("counts")]
        public async Task<ActionResult<object>> GetReportCounts()
        {
            var counts = await _context.Reports
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                pending = counts.FirstOrDefault(c => c.Status == ReportStatus.Pending)?.Count ?? 0,
                reviewed = counts.FirstOrDefault(c => c.Status == ReportStatus.Reviewed)?.Count ?? 0,
                resolved = counts.FirstOrDefault(c => c.Status == ReportStatus.Resolved)?.Count ?? 0,
                dismissed = counts.FirstOrDefault(c => c.Status == ReportStatus.Dismissed)?.Count ?? 0,
                total = counts.Sum(c => c.Count)
            });
        }

        /// <summary>
        /// Get single report by ID.
        /// GET: api/AdminReports/5
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ReportDto>> GetReport(int id)
        {
            var r = await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.TargetComment).ThenInclude(c => c!.User)
                .Include(r => r.TargetTitle)
                .Include(r => r.TargetChapter)
                .Include(r => r.TargetUser)
                .Include(r => r.ReviewedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (r == null) return NotFound();

            return Ok(new ReportDto
            {
                Id = r.Id,
                ReporterUserId = r.ReporterUserId,
                ReporterUserName = r.ReporterUser?.UserName,
                ReporterAvatar = r.ReporterUser?.ProfilePicturePath,
                TargetType = r.TargetType,
                TargetCommentId = r.TargetCommentId,
                TargetTitleId = r.TargetTitleId,
                TargetChapterId = r.TargetChapterId,
                TargetUserId = r.TargetUserId,
                TargetPreview = r.TargetType == ReportTargetType.Comment && r.TargetComment != null
                    ? r.TargetComment.Content
                    : r.TargetType == ReportTargetType.Title && r.TargetTitle != null
                        ? r.TargetTitle.EnglishTitle
                        : r.TargetType == ReportTargetType.Chapter && r.TargetChapter != null
                            ? r.TargetChapter.Name
                            : null,
                TargetUserName = r.TargetUser?.UserName,
                Reason = r.Reason,
                Description = r.Description,
                Status = r.Status,
                ReviewedByUserName = r.ReviewedByUser?.UserName,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt
            });
        }

        /// <summary>
        /// Review/resolve a report.
        /// PUT: api/AdminReports/5/review
        /// </summary>
        [HttpPut("{id}/review")]
        public async Task<ActionResult> ReviewReport(int id, [FromBody] ReviewReportDto dto)
        {
            try
            {
                var report = await _context.Reports.FindAsync(id);
                if (report == null) return NotFound();

                var adminId = _userManager.GetUserId(User);

                report.Status = dto.Status;
                report.AdminNote = dto.AdminNote;
                report.ReviewedByUserId = adminId;
                report.ReviewedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} reviewed report {ReportId} with status {Status}",
                    adminId, id, dto.Status);

                return Ok(new { message = "Report updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing report {ReportId}", id);
                return StatusCode(500, "An error occurred.");
            }
        }

        /// <summary>
        /// Bulk update report statuses.
        /// PUT: api/AdminReports/bulk-review
        /// </summary>
        [HttpPut("bulk-review")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> BulkReviewReports([FromBody] BulkReviewDto dto)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                var reports = await _context.Reports
                    .Where(r => dto.ReportIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var report in reports)
                {
                    report.Status = dto.Status;
                    report.AdminNote = dto.AdminNote;
                    report.ReviewedByUserId = adminId;
                    report.ReviewedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} bulk-reviewed {Count} reports as {Status}",
                    adminId, reports.Count, dto.Status);

                return Ok(new { message = $"{reports.Count} reports updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk reviewing reports");
                return StatusCode(500, "An error occurred.");
            }
        }
    }

    // Extra DTO for bulk
    public class BulkReviewDto
    {
        public List<int> ReportIds { get; set; } = new();
        public ReportStatus Status { get; set; }
        public string? AdminNote { get; set; }
    }
}
