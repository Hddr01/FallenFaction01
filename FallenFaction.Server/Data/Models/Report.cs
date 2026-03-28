using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public enum ReportTargetType
    {
        Comment = 1,
        Title = 2,
        Chapter = 3,
        User = 4
    }

    public enum ReportStatus
    {
        Pending = 0,
        Reviewed = 1,
        Resolved = 2,
        Dismissed = 3
    }

    public enum ReportReason
    {
        Spam = 1,
        Harassment = 2,
        InappropriateContent = 3,
        Spoiler = 4,
        CopyrightViolation = 5,
        MisinformationOrFake = 6,
        HateSpeech = 7,
        Other = 99
    }

    public class Report
    {
        [Key]
        public int Id { get; set; }

        // Who filed the report
        [Required]
        public string ReporterUserId { get; set; } = string.Empty;
        [ForeignKey("ReporterUserId")]
        public AppUser? ReporterUser { get; set; }

        // What is being reported (polymorphic)
        public ReportTargetType TargetType { get; set; }
        public int? TargetCommentId { get; set; }
        public int? TargetTitleId { get; set; }
        public int? TargetChapterId { get; set; }
        public string? TargetUserId { get; set; }

        // Navigation (optional, for eager loading)
        [ForeignKey("TargetCommentId")]
        public Comment? TargetComment { get; set; }
        [ForeignKey("TargetTitleId")]
        public Title? TargetTitle { get; set; }
        [ForeignKey("TargetChapterId")]
        public Chapter? TargetChapter { get; set; }
        [ForeignKey("TargetUserId")]
        public AppUser? TargetUser { get; set; }

        // Report details
        public ReportReason Reason { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // Admin handling
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public string? ReviewedByUserId { get; set; }
        [ForeignKey("ReviewedByUserId")]
        public AppUser? ReviewedByUser { get; set; }

        [StringLength(1000)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}
