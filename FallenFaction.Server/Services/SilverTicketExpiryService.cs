using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Services
{
    /// <summary>
    /// Background hosted service that runs every hour and expires Silver tickets
    /// whose ExpiresAt timestamp has passed. For each expired batch it:
    ///   1. Sums up the expired Silver amount per user.
    ///   2. Deducts from the user's SilverBalance (floor at 0).
    ///   3. Writes a new TicketTransaction row with TransactionType = Expiry.
    /// </summary>
    public class SilverTicketExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SilverTicketExpiryService> _logger;
        private static readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public SilverTicketExpiryService(
            IServiceScopeFactory scopeFactory,
            ILogger<SilverTicketExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SilverTicketExpiryService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunExpiryPass();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Silver ticket expiry pass.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task RunExpiryPass()
        {
            using var scope   = _scopeFactory.CreateScope();
            var context       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now           = DateTime.UtcNow;

            // Find all unexpired Silver earn transactions whose expiry has passed
            // and that haven't already been expired (no paired Expiry transaction)
            var expiredBatches = await context.TicketTransactions
                .Where(t =>
                    t.TicketType      == TicketType.Silver
                    && t.Amount       > 0                          // only earn rows
                    && t.ExpiresAt    != null
                    && t.ExpiresAt    <= now
                    && !context.TicketTransactions.Any(e =>        // no expiry already logged
                        e.UserId          == t.UserId
                        && e.TransactionType == TicketTransactionType.Expiry
                        && e.RelatedRequestId == t.Id))
                .GroupBy(t => t.UserId)
                .Select(g => new
                {
                    UserId       = g.Key,
                    TotalExpired = g.Sum(t => t.Amount),
                    TransactionIds = g.Select(t => t.Id).ToList()
                })
                .ToListAsync();

            if (!expiredBatches.Any()) return;

            foreach (var batch in expiredBatches)
            {
                var wallet = await context.UserTickets
                    .FirstOrDefaultAsync(w => w.UserId == batch.UserId);

                if (wallet == null) continue;

                var actualDeduction = Math.Min(wallet.SilverBalance, batch.TotalExpired);
                if (actualDeduction <= 0) continue;

                wallet.SilverBalance -= actualDeduction;
                wallet.UpdatedAt      = now;

                context.TicketTransactions.Add(new TicketTransaction
                {
                    UserId          = batch.UserId,
                    TicketType      = TicketType.Silver,
                    TransactionType = TicketTransactionType.Expiry,
                    Amount          = -actualDeduction,
                    BalanceAfter    = wallet.SilverBalance,
                    Description     = $"Silver tickets expired ({batch.TransactionIds.Count} batch(es))",
                    CreatedAt       = now
                });

                _logger.LogInformation("Expired {Amount} Silver tickets for user {UserId}.",
                    actualDeduction, batch.UserId);
            }

            await context.SaveChangesAsync();
        }
    }
}
