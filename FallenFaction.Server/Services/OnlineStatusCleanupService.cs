// Services/OnlineStatusCleanupService.cs - IMPROVED VERSION with reduced conflicts
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Services
{
    public class OnlineStatusCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OnlineStatusCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10); // Increased to 10 minutes to reduce conflicts
        private readonly TimeSpan _offlineThreshold = TimeSpan.FromMinutes(15); // Increased threshold
        private readonly TimeSpan _recentActivityThreshold = TimeSpan.FromMinutes(5); // Longer grace period

        public OnlineStatusCleanupService(
            IServiceProvider serviceProvider,
            ILogger<OnlineStatusCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Online Status Cleanup Service started with {Interval} minute intervals", _checkInterval.TotalMinutes);

            // Wait a bit before starting to let the application fully initialize
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupStaleOnlineStatusesAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Online Status Cleanup Service");
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
            }

            _logger.LogInformation("Online Status Cleanup Service stopped");
        }

        private async Task CleanupStaleOnlineStatusesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            try
            {
                var cutoffTime = DateTime.UtcNow.Subtract(_offlineThreshold);
                var recentActivityCutoff = DateTime.UtcNow.Subtract(_recentActivityThreshold);

                _logger.LogDebug("Starting cleanup for users inactive since {CutoffTime} (excluding recent activity since {RecentCutoff})",
                    cutoffTime, recentActivityCutoff);

                // Find stale online users with more conservative criteria
                var staleOnlineUsers = await userManager.Users
                    .Where(u => u.IsOnline &&
                               u.LastActive < cutoffTime &&
                               u.LastActive < recentActivityCutoff)
                    .Select(u => new { u.Id, u.UserName, u.LastActive }) // Select only needed fields
                    .ToListAsync();

                if (staleOnlineUsers.Any())
                {
                    _logger.LogInformation("Found {Count} stale online users to process", staleOnlineUsers.Count);

                    // Process users one by one with longer delays to reduce database load
                    foreach (var userInfo in staleOnlineUsers)
                    {
                        try
                        {
                            await ProcessSingleUserCleanupAsync(userManager, userInfo.Id);

                            // Delay between each user to reduce load
                            await Task.Delay(500); // 500ms between users
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error processing cleanup for user {UserId} ({UserName})",
                                userInfo.Id, userInfo.UserName);
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("No stale online users found during cleanup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during online status cleanup");
            }
        }

        private async Task ProcessSingleUserCleanupAsync(UserManager<AppUser> userManager, string userId)
        {
            const int maxRetries = 2; // Reduced retries for cleanup

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Get fresh user data
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        _logger.LogDebug("User {UserId} not found during cleanup attempt {Attempt}", userId, attempt + 1);
                        return;
                    }

                    // Double-check conditions before updating
                    var recentActivityCutoff = DateTime.UtcNow.Subtract(_recentActivityThreshold);
                    var offlineCutoff = DateTime.UtcNow.Subtract(_offlineThreshold);

                    if (!user.IsOnline ||
                        user.LastActive > recentActivityCutoff ||
                        user.LastActive > offlineCutoff)
                    {
                        _logger.LogDebug("User {UserId} no longer needs cleanup (online: {IsOnline}, lastActive: {LastActive})",
                            user.Id, user.IsOnline, user.LastActive);
                        return;
                    }

                    // Use a minimal update approach
                    user.IsOnline = false;
                    // Don't update LastActive during cleanup to avoid confusion

                    var result = await userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Cleanup: Set user {UserId} ({UserName}) offline due to inactivity since {LastActive}",
                            user.Id, user.UserName, user.LastActive);
                        return; // Success
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                        if (errors.Contains("concurrency", StringComparison.OrdinalIgnoreCase) && attempt < maxRetries - 1)
                        {
                            _logger.LogDebug("Concurrency conflict during cleanup for user {UserId}, retrying...", user.Id);
                            await Task.Delay(1000 * (attempt + 1)); // Longer delay for cleanup retries
                            continue;
                        }

                        _logger.LogWarning("Failed to cleanup user {UserId} after {Attempt} attempts: {Errors}",
                            user.Id, attempt + 1, errors);
                        return;
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (attempt < maxRetries - 1)
                    {
                        _logger.LogDebug("Database concurrency exception during cleanup for user {UserId}, retrying...", userId);
                        await Task.Delay(1000 * (attempt + 1));
                        continue;
                    }

                    _logger.LogWarning("Concurrency exception persisted for user {UserId} during cleanup", userId);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error during cleanup for user {UserId} on attempt {Attempt}",
                        userId, attempt + 1);

                    if (attempt == maxRetries - 1)
                    {
                        return;
                    }

                    await Task.Delay(1000 * (attempt + 1));
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Online Status Cleanup Service is stopping...");
            await base.StopAsync(stoppingToken);
            _logger.LogInformation("Online Status Cleanup Service stopped gracefully");
        }
    }
}