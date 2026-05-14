using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.AI;
using FallenFaction.Server.Services.Interfaces;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TicketsController> _logger;
        private readonly ITicketWalletService _wallet;

        public TicketsController(
            ApplicationDbContext context,
            ILogger<TicketsController> logger,
            ITicketWalletService wallet)
        {
            _context = context;
            _logger = logger;
            _wallet = wallet;
        }

        // ── GET /api/tickets/wallet ──────────────────────────────────────────
        /// <summary>Returns the current user's Silver balance + level info.</summary>
        [HttpGet("wallet")]
        public async Task<ActionResult<WalletDto>> GetWallet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users
                .Select(u => new { u.Id, u.UserLevel, u.XpPoints })
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            var wallet = await _context.UserTickets.FirstOrDefaultAsync(w => w.UserId == userId);
            var canVote = user.UserLevel >= 2;

            return Ok(new WalletDto
            {
                SilverBalance = wallet?.SilverBalance ?? 0,
                TotalBalance = wallet?.SilverBalance ?? 0,
                CanVote = canVote,
                UserLevel = user.UserLevel,
                XpPoints = user.XpPoints
            });
        }

        // ── GET /api/tickets/transactions ────────────────────────────────────
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.TicketTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();
            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    TicketType = t.TicketType.ToString(),
                    TransactionType = t.TransactionType.ToString(),
                    Amount = t.Amount,
                    BalanceAfter = t.BalanceAfter,
                    Description = t.Description,
                    RelatedTitleId = t.RelatedTitleId,
                    RelatedChapterId = t.RelatedChapterId,
                    ExpiresAt = t.ExpiresAt,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(rows);
        }

        // ── GET /api/tickets/unlock-cost/{chapterId} ─────────────────────────
        [HttpGet("unlock-cost/{chapterId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ChapterUnlockCostDto>> GetUnlockCost(int chapterId)
        {
            var chapter = await _context.Chapters
                .Where(c => c.Id == chapterId)
                .Select(c => new { c.Id, c.IsAILocked, c.CharacterCount })
                .FirstOrDefaultAsync();

            if (chapter == null) return NotFound();

            var cost = _wallet.ComputeUnlockCost(chapter.CharacterCount);
            return Ok(new ChapterUnlockCostDto
            {
                ChapterId = chapterId,
                CharacterCount = chapter.CharacterCount,
                Cost = cost,
                IsAlreadyUnlocked = !chapter.IsAILocked
            });
        }

        // ── POST /api/tickets/unlock ─────────────────────────────────────────
        /// <summary>
        /// Spend Silver tickets to unlock an AI chapter permanently for everyone.
        /// </summary>
        [HttpPost("unlock")]
        [EnableRateLimiting("ticket-unlock")]
        public async Task<ActionResult<UnlockChapterResponseDto>> UnlockChapter(
            [FromBody] UnlockChapterDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Title)
                    .FirstOrDefaultAsync(c => c.Id == dto.ChapterId);

                if (chapter == null)
                    return NotFound("Chapter not found.");

                if (!chapter.IsAILocked)
                    return BadRequest("Chapter is already unlocked.");

                // Prevent double-unlock race condition
                var alreadyUnlocked = await _context.AIChapterUnlocks
                    .AnyAsync(u => u.ChapterId == dto.ChapterId);
                if (alreadyUnlocked)
                {
                    chapter.IsAILocked = false;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Ok(new UnlockChapterResponseDto { Success = true, TicketsSpent = 0, Message = "Chapter was already unlocked by another user." });
                }

                var cost = _wallet.ComputeUnlockCost(chapter.CharacterCount);

                var existingWallet = await _context.UserTickets
                    .FirstOrDefaultAsync(w => w.UserId == userId);
                if (existingWallet == null || existingWallet.TotalBalance < cost)
                    return BadRequest($"Insufficient tickets. Need {cost:F2}, have {existingWallet?.TotalBalance ?? 0:F2}.");

                var debit = await _wallet.DebitAsync(
                    userId!,
                    cost,
                    TicketTransactionType.ChapterUnlock,
                    $"Unlocked Ch.{chapter.ChapterNumber} of {chapter.Title?.EnglishTitle}",
                    relatedTitleId: chapter.TitleId,
                    relatedChapterId: chapter.Id);

                _context.AIChapterUnlocks.Add(new AIChapterUnlock
                {
                    ChapterId = chapter.Id,
                    TitleId = chapter.TitleId,
                    UnlockedByUserId = userId!,
                    TicketCost = cost,
                    TicketTypeUsed = TicketType.Silver,
                    CharacterCount = chapter.CharacterCount,
                    UnlockedAt = DateTime.UtcNow
                });

                chapter.IsAILocked = false;

                await AwardXpAsync(userId!, 15, "Unlocked an AI chapter");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new UnlockChapterResponseDto
                {
                    Success = true,
                    TicketsSpent = cost,
                    NewSilverBalance = debit.NewSilverBalance,
                    Message = $"Chapter unlocked! Spent {cost:F2} tickets."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error unlocking chapter {ChapterId}", dto.ChapterId);
                return StatusCode(500, "An error occurred while unlocking.");
            }
        }

        // ── POST /api/tickets/admin/grant  [Admin] ───────────────────────────
        [HttpPost("admin/grant")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGrant([FromBody] AdminGrantTicketsDto dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var ticketType = TicketType.Silver;
            DateTime? expiresAt = DateTime.UtcNow.AddMonths(dto.ExpiryMonths ?? 3);

            await _wallet.CreditAsync(
                dto.UserId,
                ticketType,
                dto.Amount,
                TicketTransactionType.AdminGrant,
                dto.Description,
                expiresAt: expiresAt,
                performedByUserId: adminId);

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Granted {dto.Amount} {ticketType} tickets to user {dto.UserId}." });
        }

        // ── POST /api/tickets/xp/award  [Internal helper — Admin only] ───────
        [HttpPost("admin/award-xp")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAwardXp([FromBody] AdminAwardXpDto dto)
        {
            await AwardXpAsync(dto.UserId, dto.Amount, dto.Reason);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Awarded {dto.Amount} XP to {dto.UserId}." });
        }

        // ── GET /api/tickets/admin/grant-log  [Admin] ────────────────────────
        /// <summary>Returns the most recent AdminGrant transactions across all users.</summary>
        [HttpGet("admin/grant-log")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGrantLog([FromQuery] int limit = 20)
        {
            var rows = await _context.TicketTransactions
                .Where(t => t.TransactionType == TicketTransactionType.AdminGrant)
                .OrderByDescending(t => t.CreatedAt)
                .Take(Math.Min(limit, 100))
                .Select(t => new
                {
                    t.Id,
                    t.UserId,
                    UserName = _context.Users
                        .Where(u => u.Id == t.UserId)
                        .Select(u => u.UserName)
                        .FirstOrDefault(),
                    TicketType = t.TicketType.ToString(),
                    t.Amount,
                    t.Description,
                    t.PerformedByUserId,
                    t.ExpiresAt,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(rows);
        }

        // ── GET /api/tickets/admin/user-search  [Admin] ──────────────────────
        /// <summary>Quick user lookup for the grant UI — returns id, userName, email.</summary>
        [HttpGet("admin/search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUserSearch([FromQuery] string q, [FromQuery] int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(Array.Empty<object>());
            if (q.Length > 100)
                return BadRequest(new { message = "Search query must not exceed 100 characters." });

            var lower = q.ToLower();
            var users = await _context.Users
                .Where(u => u.UserName!.ToLower().Contains(lower) || u.Email!.ToLower().Contains(lower))
                .Take(Math.Min(limit, 20))
                .Select(u => new { u.Id, u.UserName, u.Email })
                .ToListAsync();

            return Ok(users);
        }

        // ── Internal: AwardXp ────────────────────────────────────────────────
        public async Task AwardXpAsync(string userId, int xpAmount, string reason)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            user.XpPoints += xpAmount;
            user.UserLevel = AppUser.ComputeLevel(user.XpPoints);

            _logger.LogDebug("Awarded {Xp} XP to {UserId} — {Reason}. New level: {Level}",
                xpAmount, userId, reason, user.UserLevel);
        }

    }

    // Small DTO only used internally by the admin endpoint
    public class AdminAwardXpDto
    {
        [Required, StringLength(36)]
        public string UserId { get; set; } = string.Empty;

        [Range(1, 10000)]
        public int Amount { get; set; }

        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;
    }
}
