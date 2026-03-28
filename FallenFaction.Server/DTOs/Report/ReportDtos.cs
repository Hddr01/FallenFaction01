using System.ComponentModel.DataAnnotations;
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Report
{
    // ── Create report (user submits) ──────────────────────────────────────
    public class CreateReportDto
    {
        [Required]
        public ReportTargetType TargetType { get; set; }

        public int? TargetCommentId { get; set; }
        public int? TargetTitleId { get; set; }
        public int? TargetChapterId { get; set; }
        public string? TargetUserId { get; set; }

        [Required]
        public ReportReason Reason { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }

    // ── Admin review ──────────────────────────────────────────────────────
    public class ReviewReportDto
    {
        [Required]
        public ReportStatus Status { get; set; }

        [StringLength(1000)]
        public string? AdminNote { get; set; }
    }

    // ── Response ──────────────────────────────────────────────────────────
    public class ReportDto
    {
        public int Id { get; set; }

        // Reporter
        public string ReporterUserId { get; set; } = string.Empty;
        public string? ReporterUserName { get; set; }
        public string? ReporterAvatar { get; set; }

        // Target
        public ReportTargetType TargetType { get; set; }
        public string TargetTypeName => TargetType.ToString();
        public int? TargetCommentId { get; set; }
        public int? TargetTitleId { get; set; }
        public int? TargetChapterId { get; set; }
        public string? TargetUserId { get; set; }

        // Preview of target content
        public string? TargetPreview { get; set; }
        public string? TargetUserName { get; set; }

        // Details
        public ReportReason Reason { get; set; }
        public string ReasonName => Reason.ToString();
        public string? Description { get; set; }

        // Status
        public ReportStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public string? ReviewedByUserName { get; set; }
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    // ── Paginated response ───────────────────────────────────────────────
    public class ReportsPagedResponse
    {
        public List<ReportDto> Reports { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
