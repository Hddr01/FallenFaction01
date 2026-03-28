using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get notifications for current user (personal + global unread).
        /// GET: api/Notifications?page=1&pageSize=20
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<NotificationsPagedResponse>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var now = DateTime.UtcNow;

                // IDs of global notifications this user already dismissed
                var dismissedGlobalIds = await _context.UserNotificationReads
                    .Where(unr => unr.UserId == userId && unr.IsDismissed)
                    .Select(unr => unr.NotificationId)
                    .ToListAsync();

                // IDs of global notifications this user has read
                var readGlobalIds = await _context.UserNotificationReads
                    .Where(unr => unr.UserId == userId)
                    .Select(unr => unr.NotificationId)
                    .ToListAsync();

                // Build query: personal + active global (not expired, not dismissed)
                var query = _context.Notifications
                    .Where(n =>
                        // Personal notifications for this user
                        (n.UserId == userId) ||
                        // Global notifications that are active and not dismissed
                        (n.IsGlobal && !dismissedGlobalIds.Contains(n.Id) &&
                         (n.ExpiresAt == null || n.ExpiresAt > now) &&
                         (n.ScheduledFor == null || n.ScheduledFor <= now)))
                    .AsQueryable();

                if (unreadOnly)
                {
                    query = query.Where(n =>
                        (n.UserId == userId && !n.IsRead) ||
                        (n.IsGlobal && !readGlobalIds.Contains(n.Id)));
                }

                var totalCount = await query.CountAsync();

                // Count unread
                var unreadCount = await _context.Notifications
                    .Where(n =>
                        (n.UserId == userId && !n.IsRead) ||
                        (n.IsGlobal && !readGlobalIds.Contains(n.Id) &&
                         !dismissedGlobalIds.Contains(n.Id) &&
                         (n.ExpiresAt == null || n.ExpiresAt > now) &&
                         (n.ScheduledFor == null || n.ScheduledFor <= now)))
                    .CountAsync();

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        LinkUrl = n.LinkUrl,
                        RelatedTitleId = n.RelatedTitleId,
                        RelatedChapterId = n.RelatedChapterId,
                        IsRead = n.IsGlobal
                            ? readGlobalIds.Contains(n.Id)
                            : n.IsRead,
                        IsGlobal = n.IsGlobal,
                        CreatedAt = n.CreatedAt,
                        ScheduledFor = n.ScheduledFor,
                        ExpiresAt = n.ExpiresAt
                    })
                    .ToListAsync();

                return Ok(new NotificationsPagedResponse
                {
                    Notifications = notifications,
                    TotalCount = totalCount,
                    UnreadCount = unreadCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return StatusCode(500, "An error occurred.");
            }
        }

        /// <summary>
        /// Get unread count for badge display.
        /// GET: api/Notifications/unread-count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var now = DateTime.UtcNow;
            var readGlobalIds = await _context.UserNotificationReads
                .Where(unr => unr.UserId == userId)
                .Select(unr => unr.NotificationId)
                .ToListAsync();

            var dismissedGlobalIds = await _context.UserNotificationReads
                .Where(unr => unr.UserId == userId && unr.IsDismissed)
                .Select(unr => unr.NotificationId)
                .ToListAsync();

            var count = await _context.Notifications
                .Where(n =>
                    (n.UserId == userId && !n.IsRead) ||
                    (n.IsGlobal && !readGlobalIds.Contains(n.Id) &&
                     !dismissedGlobalIds.Contains(n.Id) &&
                     (n.ExpiresAt == null || n.ExpiresAt > now) &&
                     (n.ScheduledFor == null || n.ScheduledFor <= now)))
                .CountAsync();

            return Ok(count);
        }

        /// <summary>
        /// Mark a notification as read.
        /// PUT: api/Notifications/5/read
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            if (notification.IsGlobal)
            {
                // For global notifications, create/update the read record
                var readRecord = await _context.UserNotificationReads
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.NotificationId == id);

                if (readRecord == null)
                {
                    _context.UserNotificationReads.Add(new UserNotificationRead
                    {
                        UserId = userId,
                        NotificationId = id,
                        ReadAt = DateTime.UtcNow
                    });
                }
            }
            else if (notification.UserId == userId)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
            else
            {
                return Forbid();
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Mark all notifications as read.
        /// PUT: api/Notifications/read-all
        /// </summary>
        [HttpPut("read-all")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Mark personal notifications
            var personalUnread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in personalUnread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }

            // Mark global notifications
            var now = DateTime.UtcNow;
            var readGlobalIds = await _context.UserNotificationReads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToListAsync();

            var unreadGlobalIds = await _context.Notifications
                .Where(n => n.IsGlobal && !readGlobalIds.Contains(n.Id) &&
                            (n.ExpiresAt == null || n.ExpiresAt > now))
                .Select(n => n.Id)
                .ToListAsync();

            foreach (var gId in unreadGlobalIds)
            {
                _context.UserNotificationReads.Add(new UserNotificationRead
                {
                    UserId = userId,
                    NotificationId = gId,
                    ReadAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "All notifications marked as read." });
        }

        /// <summary>
        /// Dismiss a global notification (hide it permanently).
        /// PUT: api/Notifications/5/dismiss
        /// </summary>
        [HttpPut("{id}/dismiss")]
        public async Task<ActionResult> DismissNotification(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            if (notification.IsGlobal)
            {
                var readRecord = await _context.UserNotificationReads
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.NotificationId == id);

                if (readRecord == null)
                {
                    _context.UserNotificationReads.Add(new UserNotificationRead
                    {
                        UserId = userId,
                        NotificationId = id,
                        ReadAt = DateTime.UtcNow,
                        IsDismissed = true
                    });
                }
                else
                {
                    readRecord.IsDismissed = true;
                }
            }
            else if (notification.UserId == userId)
            {
                _context.Notifications.Remove(notification);
            }
            else
            {
                return Forbid();
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
