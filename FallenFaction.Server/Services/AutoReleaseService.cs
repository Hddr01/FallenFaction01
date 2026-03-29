using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Services
{
    /// <summary>
    /// Background hosted service that fires every 2 hours.
    /// Finds the Approved TranslationRequest with the highest VoteCount
    /// and advances it to PreProcessing status — signalling the admin
    /// (or a future pipeline) that it's ready to be released.
    ///
    /// NOTE: Actual Title creation + Release confirmation is done manually
    /// by the admin via POST /api/translation-requests/admin/release.
    /// This job only handles the auto-PreProcessing trigger.
    /// </summary>
    public class AutoReleaseService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoReleaseService> _logger;
        private static readonly TimeSpan _interval = TimeSpan.FromHours(2);

        public AutoReleaseService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoReleaseService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoReleaseService started (2h interval).");

            // Don't fire immediately on startup — wait one full cycle
            await Task.Delay(_interval, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TriggerTopVotedRelease();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during auto-release trigger.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task TriggerTopVotedRelease()
        {
            using var scope = _scopeFactory.CreateScope();
            var context     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Find the top-voted Approved request
            var top = await context.TranslationRequests
                .Where(r => r.Status == TranslationRequestStatus.Approved && r.VoteCount > 0)
                .OrderByDescending(r => r.VoteCount)
                .ThenBy(r => r.CreatedAt)   // oldest wins tie-break
                .FirstOrDefaultAsync();

            if (top == null)
            {
                _logger.LogDebug("AutoRelease: no eligible Approved requests found.");
                return;
            }

            top.Status    = TranslationRequestStatus.PreProcessing;
            top.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation(
                "AutoRelease: advanced request {Id} '{Title}' ({Votes} votes) → PreProcessing.",
                top.Id, top.ProposedTitle, top.VoteCount);
        }
    }
}
