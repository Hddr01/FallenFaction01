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
    [Authorize(Roles = "Admin")]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AdminNotificationsController> _logger;

        public AdminNotificationsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<AdminNotificationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Send a global notification to all users.
        /// POST: api/AdminNotifications/global
        /// </summary>
        [HttpPost("global")]
        public async Task<ActionResult> SendGlobalNotification([FromBody] CreateGlobalNotificationDto dto)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);

                var notification = new Notification
                {
                    UserId = null, // null = global
                    Type = dto.Type,
                    Title = dto.Title,
                    Message = dto.Message,
                    LinkUrl = dto.LinkUrl,
                    IsGlobal = true,
                    CreatedByUserId = adminId,
                    ScheduledFor = dto.ScheduledFor,
                    ExpiresAt = dto.ExpiresAt,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} sent global notification: {Title}", adminId, dto.Title);

                return Ok(new { id = notification.Id, message = "Global notification sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending global notification");
                return StatusCode(500, "An error occurred.");
            }
        }

        /// <summary>
        /// Get all global notifications (admin view).
        /// GET: api/AdminNotifications/global
        /// </summary>
        [HttpGet("global")]
        public async Task<ActionResult<List<NotificationDto>>> GetGlobalNotifications()
        {
            var notifications = await _context.Notifications
                .Where(n => n.IsGlobal)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    LinkUrl = n.LinkUrl,
                    IsGlobal = true,
                    CreatedAt = n.CreatedAt,
                    ScheduledFor = n.ScheduledFor,
                    ExpiresAt = n.ExpiresAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        /// <summary>
        /// Delete a global notification.
        /// DELETE: api/AdminNotifications/global/5
        /// </summary>
        [HttpDelete("global/{id}")]
        public async Task<ActionResult> DeleteGlobalNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null || !notification.IsGlobal)
                return NotFound();

            // Also remove read records
            var readRecords = await _context.UserNotificationReads
                .Where(r => r.NotificationId == id)
                .ToListAsync();

            _context.UserNotificationReads.RemoveRange(readRecords);
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Global notification deleted." });
        }

        /// <summary>
        /// Update a global notification.
        /// PUT: api/AdminNotifications/global/5
        /// </summary>
        [HttpPut("global/{id}")]
        public async Task<ActionResult> UpdateGlobalNotification(int id, [FromBody] CreateGlobalNotificationDto dto)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null || !notification.IsGlobal)
                return NotFound();

            notification.Type = dto.Type;
            notification.Title = dto.Title;
            notification.Message = dto.Message;
            notification.LinkUrl = dto.LinkUrl;
            notification.ScheduledFor = dto.ScheduledFor;
            notification.ExpiresAt = dto.ExpiresAt;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Notification updated." });
        }
    }
}
