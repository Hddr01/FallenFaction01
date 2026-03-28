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
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Submit a new report (any authenticated user).
        /// POST: api/Reports
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ReportDto>> CreateReport([FromBody] CreateReportDto dto)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Validate that the target exists
                switch (dto.TargetType)
                {
                    case ReportTargetType.Comment:
                        if (dto.TargetCommentId == null || !await _context.Comments.AnyAsync(c => c.Id == dto.TargetCommentId))
                            return BadRequest("Invalid comment ID.");
                        break;
                    case ReportTargetType.Title:
                        if (dto.TargetTitleId == null || !await _context.Titles.AnyAsync(t => t.Id == dto.TargetTitleId))
                            return BadRequest("Invalid title ID.");
                        break;
                    case ReportTargetType.Chapter:
                        if (dto.TargetChapterId == null || !await _context.Chapters.AnyAsync(c => c.Id == dto.TargetChapterId))
                            return BadRequest("Invalid chapter ID.");
                        break;
                    case ReportTargetType.User:
                        if (string.IsNullOrEmpty(dto.TargetUserId) || !await _context.Users.AnyAsync(u => u.Id == dto.TargetUserId))
                            return BadRequest("Invalid user ID.");
                        break;
                    default:
                        return BadRequest("Invalid target type.");
                }

                // Prevent duplicate reports from the same user on the same target
                var existingReport = await _context.Reports.AnyAsync(r =>
                    r.ReporterUserId == userId &&
                    r.TargetType == dto.TargetType &&
                    r.TargetCommentId == dto.TargetCommentId &&
                    r.TargetTitleId == dto.TargetTitleId &&
                    r.TargetChapterId == dto.TargetChapterId &&
                    r.TargetUserId == dto.TargetUserId &&
                    r.Status == ReportStatus.Pending);

                if (existingReport)
                    return Conflict("You have already reported this item. It is pending review.");

                var report = new Report
                {
                    ReporterUserId = userId,
                    TargetType = dto.TargetType,
                    TargetCommentId = dto.TargetCommentId,
                    TargetTitleId = dto.TargetTitleId,
                    TargetChapterId = dto.TargetChapterId,
                    TargetUserId = dto.TargetUserId,
                    Reason = dto.Reason,
                    Description = dto.Description,
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created report {ReportId} for {TargetType}",
                    userId, report.Id, dto.TargetType);

                return Ok(new { id = report.Id, message = "Report submitted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                return StatusCode(500, "An error occurred while submitting the report.");
            }
        }

        /// <summary>
        /// Get the current user's own reports.
        /// GET: api/Reports/my
        /// </summary>
        [HttpGet("my")]
        public async Task<ActionResult<List<ReportDto>>> GetMyReports()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var reports = await _context.Reports
                .Where(r => r.ReporterUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .Select(r => new ReportDto
                {
                    Id = r.Id,
                    ReporterUserId = r.ReporterUserId,
                    TargetType = r.TargetType,
                    TargetCommentId = r.TargetCommentId,
                    TargetTitleId = r.TargetTitleId,
                    TargetChapterId = r.TargetChapterId,
                    TargetUserId = r.TargetUserId,
                    Reason = r.Reason,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ReviewedAt = r.ReviewedAt,
                    AdminNote = r.AdminNote
                })
                .ToListAsync();

            return Ok(reports);
        }
    }
}
