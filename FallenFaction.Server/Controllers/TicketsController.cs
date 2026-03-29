using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.AI;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(ApplicationDbContext context, ILogger<TicketsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── GET /api/tickets/wallet ──────────────────────────────────────────
        /// <summary>Returns the current user's Gold + Silver balance + level info.</summary>
        [HttpGet("wallet")]
        public async Task<ActionResult<WalletDto>> GetWallet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users
                .Select(u => new { u.Id, u.UserLevel, u.XpPoints, u.PatreonUserId })
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            var wallet = await _context.UserTickets.FirstOrDefaultAsync(w => w.UserId == userId);
            var canVote = user.UserLevel >= 2 || user.PatreonUserId != null;

            return Ok(new WalletDto
            {
                GoldBalance = wallet?.GoldBalance ?? 0,
                SilverBalance = wallet?.SilverBalance ?? 0,
                TotalBalance = (wallet?.GoldBalance ?? 0) + (wallet?.SilverBalance ?? 0),
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
                    PatreonTierName = t.PatreonTierName,
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

            var cost = ComputeUnlockCost(chapter.CharacterCount);
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
        /// Spend tickets to unlock an AI chapter permanently for everyone.
        /// Silver tickets are spent first, then Gold.
        /// </summary>
        [HttpPost("unlock")]
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

                var cost = ComputeUnlockCost(chapter.CharacterCount);

                var wallet = await _context.UserTickets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null || wallet.TotalBalance < cost)
                    return BadRequest($"Insufficient tickets. Need {cost:F2}, have {wallet?.TotalBalance ?? 0:F2}.");

                // Deduct Silver first, then Gold
                decimal silverSpent = 0, goldSpent = 0;
                if (wallet.SilverBalance >= cost)
                {
                    silverSpent = cost;
                    wallet.SilverBalance -= cost;
                }
                else
                {
                    silverSpent = wallet.SilverBalance;
                    goldSpent = cost - silverSpent;
                    wallet.SilverBalance = 0;
                    wallet.GoldBalance -= goldSpent;
                }
                wallet.UpdatedAt = DateTime.UtcNow;

                // Record ledger entries
                if (silverSpent > 0)
                    _context.TicketTransactions.Add(new TicketTransaction
                    {
                        UserId = userId!,
                        TicketType = TicketType.Silver,
                        TransactionType = TicketTransactionType.ChapterUnlock,
                        Amount = -silverSpent,
                        BalanceAfter = wallet.SilverBalance,
                        Description = $"Unlocked Ch.{chapter.ChapterNumber} of {chapter.Title?.EnglishTitle}",
                        RelatedChapterId = chapter.Id,
                        RelatedTitleId = chapter.TitleId,
                        CreatedAt = DateTime.UtcNow
                    });

                if (goldSpent > 0)
                    _context.TicketTransactions.Add(new TicketTransaction
                    {
                        UserId = userId!,
                        TicketType = TicketType.Gold,
                        TransactionType = TicketTransactionType.ChapterUnlock,
                        Amount = -goldSpent,
                        BalanceAfter = wallet.GoldBalance,
                        Description = $"Unlocked Ch.{chapter.ChapterNumber} of {chapter.Title?.EnglishTitle}",
                        RelatedChapterId = chapter.Id,
                        RelatedTitleId = chapter.TitleId,
                        CreatedAt = DateTime.UtcNow
                    });

                // Record unlock event
                _context.AIChapterUnlocks.Add(new AIChapterUnlock
                {
                    ChapterId = chapter.Id,
                    TitleId = chapter.TitleId,
                    UnlockedByUserId = userId!,
                    TicketCost = cost,
                    TicketTypeUsed = goldSpent > 0 ? TicketType.Gold : TicketType.Silver,
                    CharacterCount = chapter.CharacterCount,
                    UnlockedAt = DateTime.UtcNow
                });

                // Flip the lock permanently
                chapter.IsAILocked = false;

                // Award XP to the unlocker
                await AwardXpAsync(userId!, 15, "Unlocked an AI chapter");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new UnlockChapterResponseDto
                {
                    Success = true,
                    TicketsSpent = cost,
                    NewGoldBalance = wallet.GoldBalance,
                    NewSilverBalance = wallet.SilverBalance,
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

            var wallet = await _context.UserTickets.FirstOrDefaultAsync(w => w.UserId == dto.UserId);
            if (wallet == null)
            {
                wallet = new UserTicket { UserId = dto.UserId, CreatedAt = DateTime.UtcNow };
                _context.UserTickets.Add(wallet);
            }

            var ticketType = dto.TicketType.Equals("Silver", StringComparison.OrdinalIgnoreCase)
                ? TicketType.Silver : TicketType.Gold;

            DateTime? expiresAt = null;
            if (ticketType == TicketType.Silver)
            {
                wallet.SilverBalance += dto.Amount;
                expiresAt = DateTime.UtcNow.AddMonths(dto.ExpiryMonths ?? 3);
            }
            else
            {
                wallet.GoldBalance += dto.Amount;
            }
            wallet.UpdatedAt = DateTime.UtcNow;

            _context.TicketTransactions.Add(new TicketTransaction
            {
                UserId = dto.UserId,
                TicketType = ticketType,
                TransactionType = TicketTransactionType.AdminGrant,
                Amount = dto.Amount,
                BalanceAfter = ticketType == TicketType.Silver ? wallet.SilverBalance : wallet.GoldBalance,
                Description = dto.Description,
                PerformedByUserId = adminId,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            });

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

        // ── Static helper ─────────────────────────────────────────────────────
        public static decimal ComputeUnlockCost(int characterCount)
        {
            var raw = (characterCount + 500) * 0.0012m;
            return Math.Max(1m, Math.Round(raw, 2));
        }
    }

    // Small DTO only used internally by the admin endpoint
    public class AdminAwardXpDto
    {
        public string UserId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
