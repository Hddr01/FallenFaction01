using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public enum NotificationType
    {
        // Bookmark/title notifications
        NewChapter = 1,
        TitleStatusChanged = 2,

        // Global admin notifications
        GlobalAnnouncement = 10,
        MaintenanceNotice = 11,
        NewFeature = 12,

        // Social notifications (future)
        CommentReply = 20,
        CommentMention = 21,
        TeamInvite = 30,
        ReportResolved = 40
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // Null = global notification for all users
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        public NotificationType Type { get; set; }

        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        // Optional link for the notification to navigate to
        [StringLength(500)]
        public string? LinkUrl { get; set; }

        // Related entity IDs (optional context)
        public int? RelatedTitleId { get; set; }
        public int? RelatedChapterId { get; set; }

        public bool IsRead { get; set; } = false;
        public bool IsGlobal { get; set; } = false;

        // For admin who sent it
        public string? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public AppUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // For scheduled notifications (e.g. maintenance at X time)
        public DateTime? ScheduledFor { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Tracks which global notifications a user has read/dismissed.
    /// </summary>
    public class UserNotificationRead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        public int NotificationId { get; set; }
        [ForeignKey("NotificationId")]
        public Notification? Notification { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
        public bool IsDismissed { get; set; } = false;
    }
}
