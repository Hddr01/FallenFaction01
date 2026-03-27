// Services/TrustService.cs
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FallenFaction.Server.Services
{
    public class TrustService : ITrustService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrustService> _logger;

        /// <summary>
        /// Number of consecutive admin approvals required before a user is auto-trusted.
        /// </summary>
        public int TrustThreshold => 5;

        public TrustService(ApplicationDbContext context, ILogger<TrustService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsTrustedAsync(string userId, TrustActionType action)
        {
            var record = await _context.UserTrustRecords
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ActionType == action);
            return record?.IsTrusted ?? false;
        }

        public async Task RecordApprovalAsync(string userId, TrustActionType action)
        {
            var record = await GetOrCreateRecordAsync(userId, action);

            record.AdminApprovedCount++;
            record.UpdatedAt = DateTime.UtcNow;

            if (!record.IsTrusted && record.AdminApprovedCount >= TrustThreshold)
            {
                record.IsTrusted = true;
                record.TrustedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "User {UserId} has become TRUSTED for action {Action} after {Count} approvals.",
                    userId, action, record.AdminApprovedCount);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RecordRejectionAsync(string userId, TrustActionType action)
        {
            var record = await GetOrCreateRecordAsync(userId, action);

            bool wasTrusted = record.IsTrusted;
            record.AdminApprovedCount = 0;
            record.IsTrusted = false;
            record.TrustedAt = null;
            record.UpdatedAt = DateTime.UtcNow;

            if (wasTrusted)
            {
                _logger.LogInformation(
                    "User {UserId} had TRUSTED status for action {Action} REVOKED due to rejection.",
                    userId, action);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<UserTrustRecord?> GetRecordAsync(string userId, TrustActionType action)
        {
            return await _context.UserTrustRecords
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ActionType == action);
        }

        // ── helpers ─────────────────────────────────────────────────────────────

        private async Task<UserTrustRecord> GetOrCreateRecordAsync(string userId, TrustActionType action)
        {
            var record = await _context.UserTrustRecords
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ActionType == action);

            if (record == null)
            {
                record = new UserTrustRecord
                {
                    UserId = userId,
                    ActionType = action,
                    AdminApprovedCount = 0,
                    IsTrusted = false,
                };
                _context.UserTrustRecords.Add(record);
                // Don't SaveChanges here — caller will do it
            }

            return record;
        }
    }
}