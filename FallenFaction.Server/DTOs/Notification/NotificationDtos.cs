using System.ComponentModel.DataAnnotations;
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Notification
{
    // ── Admin creates global notification ─────────────────────────────────
    public class CreateGlobalNotificationDto
    {
        [Required]
        public NotificationType Type { get; set; }

        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(500)]
        public string? LinkUrl { get; set; }

        public DateTime? ScheduledFor { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    // ── Notification response ─────────────────────────────────────────────
    public class NotificationDto
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public string TypeName => Type.ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public int? RelatedTitleId { get; set; }
        public int? RelatedChapterId { get; set; }
        public bool IsRead { get; set; }
        public bool IsGlobal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    // ── Paginated list ────────────────────────────────────────────────────
    public class NotificationsPagedResponse
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
