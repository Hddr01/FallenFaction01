using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Services
{
    public sealed class TicketWalletService : ITicketWalletService
    {
        private readonly ApplicationDbContext _db;

        public TicketWalletService(ApplicationDbContext db) => _db = db;

        public decimal ComputeUnlockCost(int characterCount)
        {
            var raw = (characterCount + 500) * 0.0012m;
            return Math.Max(1m, Math.Round(raw, 2));
        }

        public async Task<UserTicket> GetOrCreateWalletAsync(string userId, CancellationToken ct = default)
        {
            var wallet = await _db.UserTickets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
            if (wallet != null) return wallet;

            wallet = new UserTicket { UserId = userId, CreatedAt = DateTime.UtcNow };
            _db.UserTickets.Add(wallet);
            return wallet;
        }

        public async Task<TicketDebitResult> DebitAsync(
            string userId,
            decimal cost,
            TicketTransactionType transactionType,
            string description,
            int? relatedTitleId = null,
            int? relatedChapterId = null,
            CancellationToken ct = default)
        {
            var wallet = await GetOrCreateWalletAsync(userId, ct);
            if (wallet.SilverBalance < cost)
                throw new InvalidOperationException(
                    $"Insufficient tickets for user {userId}: need {cost}, have {wallet.SilverBalance}.");

            wallet.SilverBalance -= cost;
            wallet.UpdatedAt = DateTime.UtcNow;

            var now = DateTime.UtcNow;
            if (cost > 0)
                _db.TicketTransactions.Add(new TicketTransaction
                {
                    UserId = userId,
                    TicketType = TicketType.Silver,
                    TransactionType = transactionType,
                    Amount = -cost,
                    BalanceAfter = wallet.SilverBalance,
                    Description = description,
                    RelatedTitleId = relatedTitleId,
                    RelatedChapterId = relatedChapterId,
                    CreatedAt = now
                });

            return new TicketDebitResult(cost, wallet.SilverBalance);
        }

        public async Task<decimal> DebitSilverCappedAsync(
            string userId,
            decimal amount,
            TicketTransactionType transactionType,
            string description,
            CancellationToken ct = default)
        {
            if (amount <= 0) return 0;

            var wallet = await _db.UserTickets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
            if (wallet == null) return 0;

            var actualDeduction = Math.Min(wallet.SilverBalance, amount);
            if (actualDeduction <= 0) return 0;

            var now = DateTime.UtcNow;
            wallet.SilverBalance -= actualDeduction;
            wallet.UpdatedAt = now;

            _db.TicketTransactions.Add(new TicketTransaction
            {
                UserId = userId,
                TicketType = TicketType.Silver,
                TransactionType = transactionType,
                Amount = -actualDeduction,
                BalanceAfter = wallet.SilverBalance,
                Description = description,
                CreatedAt = now
            });

            return actualDeduction;
        }

        public async Task CreditAsync(
            string userId,
            TicketType type,
            decimal amount,
            TicketTransactionType transactionType,
            string description,
            DateTime? expiresAt = null,
            string? performedByUserId = null,
            CancellationToken ct = default)
        {
            var wallet = await GetOrCreateWalletAsync(userId, ct);

            wallet.SilverBalance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            _db.TicketTransactions.Add(new TicketTransaction
            {
                UserId = userId,
                TicketType = type,
                TransactionType = transactionType,
                Amount = amount,
                BalanceAfter = wallet.SilverBalance,
                Description = description,
                ExpiresAt = expiresAt,
                PerformedByUserId = performedByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
