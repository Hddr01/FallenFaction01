using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Services.Interfaces
{
    public interface ITicketWalletService
    {
        // Pure helper: cost (in tickets) to unlock a chapter of the given size.
        decimal ComputeUnlockCost(int characterCount);

        // Loads the wallet for `userId`, creating an empty row if none exists.
        // Caller owns SaveChanges (the new wallet is tracked, not yet persisted).
        Task<UserTicket> GetOrCreateWalletAsync(string userId, CancellationToken ct = default);

        // Spends `cost` Silver-then-Gold. Mutates the wallet, writes ledger row(s).
        // Caller owns SaveChanges and any surrounding transaction.
        // Throws InvalidOperationException if the combined balance is below `cost`.
        Task<TicketDebitResult> DebitSilverThenGoldAsync(
            string userId,
            decimal cost,
            TicketTransactionType transactionType,
            string description,
            int? relatedTitleId = null,
            int? relatedChapterId = null,
            CancellationToken ct = default);

        // Debits Silver only, capped at the current SilverBalance. Writes one ledger
        // row when the deducted amount is positive. Returns the actual deducted amount
        // (0 if the wallet is empty or `amount` <= 0). Used by the expiry sweep.
        Task<decimal> DebitSilverCappedAsync(
            string userId,
            decimal amount,
            TicketTransactionType transactionType,
            string description,
            CancellationToken ct = default);

        // Credits the wallet with `amount` of `type`. Writes one ledger row.
        // Caller owns SaveChanges.
        Task CreditAsync(
            string userId,
            TicketType type,
            decimal amount,
            TicketTransactionType transactionType,
            string description,
            DateTime? expiresAt = null,
            string? performedByUserId = null,
            string? patreonTierName = null,
            CancellationToken ct = default);
    }

    public readonly record struct TicketDebitResult(
        decimal SilverSpent,
        decimal GoldSpent,
        decimal NewSilverBalance,
        decimal NewGoldBalance);
}
