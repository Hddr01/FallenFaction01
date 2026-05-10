using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FallenFaction.Server.Services
{
    public sealed class ApprovalCoordinator : IApprovalCoordinator
    {
        private readonly ApplicationDbContext _db;
        private readonly ITrustService _trust;
        private readonly ILogger<ApprovalCoordinator> _logger;

        public ApprovalCoordinator(
            ApplicationDbContext db,
            ITrustService trust,
            ILogger<ApprovalCoordinator> logger)
        {
            _db = db;
            _trust = trust;
            _logger = logger;
        }

        // ── AddTitle ────────────────────────────────────────────────────────

        public async Task<ApprovalOutcome<Title>> ApproveAddTitleAsync(
            int pendingTitleId, AppUser admin, CancellationToken ct = default)
        {
            var pending = await _db.Set<PendingTitle>()
                .Include(t => t.Authors)
                .Include(t => t.Artists)
                .Include(t => t.Publishers)
                .Include(t => t.Teams)
                .Include(t => t.Categories)
                .Include(t => t.Tags)
                .Include(t => t.Formats)
                .FirstOrDefaultAsync(t => t.Id == pendingTitleId, ct);

            if (pending == null)
                return ApprovalOutcome<Title>.NotFound("Pending title not found");

            if (string.IsNullOrEmpty(pending.CreatedByUserId))
                return ApprovalOutcome<Title>.BadRequest("Pending title has no associated creator");

            Title title = null!;
            await ExecuteInTransactionAsync(async () =>
            {
                title = new Title
                {
                    OriginalTitle = pending.OriginalTitle,
                    EnglishTitle = pending.EnglishTitle,
                    AlternativeNames = pending.AlternativeNames,
                    ReleaseDate = pending.ReleaseDate,
                    Description = pending.Description,
                    StatusTitle = pending.StatusTitle,
                    StatusTranslation = pending.StatusTranslation,
                    Type = pending.Type,
                    AgeRestriction = pending.AgeRestriction,
                    CoverImagePath = pending.CoverImagePath,
                    BackgroundImagePath = pending.BackgroundImagePath,
                    ExternalLinksSerialized = pending.ExternalLinksSerialized,
                    CreatedByUserId = pending.CreatedByUserId,
                    CreatedAt = DateTime.UtcNow,
                    Authors = pending.Authors,
                    Artists = pending.Artists,
                    Publishers = pending.Publishers,
                    Teams = pending.Teams,
                    Categories = pending.Categories,
                    Tags = pending.Tags,
                    Formats = pending.Formats
                };
                _db.Set<Title>().Add(title);
                await _db.SaveChangesAsync(ct);

                var pendingChapters = await _db.PendingChapters
                    .Where(pc => pc.PendingTitleId == pending.Id)
                    .ToListAsync(ct);
                foreach (var pc in pendingChapters)
                {
                    pc.TitleId = title.Id;
                    pc.PendingTitleId = null;
                }

                _db.Set<PendingTitle>().Remove(pending);

                _db.TitleChangeLogs.Add(new TitleChangeLog
                {
                    TitleId = title.Id,
                    UpdatedByUserId = pending.CreatedByUserId,
                    ReviewedByUserId = admin.Id,
                    CreatedAt = pending.CreatedAt,
                    ReviewedAt = DateTime.UtcNow,
                    ChangeType = "Add Title",
                    OldValue = "",
                    NewValue = title.OriginalTitle,
                    AdminComment = "Approved by admin",
                    Status = ChangeLogStatus.Approved,
                });

                await _db.SaveChangesAsync(ct);
                await _trust.RecordApprovalAsync(pending.CreatedByUserId, TrustActionType.AddTitle);
            }, ct);

            _logger.LogInformation("Pending title approved: {Title}", title.EnglishTitle);
            return ApprovalOutcome<Title>.Ok(title);
        }

        public async Task<ApprovalOutcome<RejectedTitle>> RejectAddTitleAsync(
            int pendingTitleId, AppUser admin, string? reason, CancellationToken ct = default)
        {
            var pending = await _db.Set<PendingTitle>()
                .Include(t => t.Authors)
                .Include(t => t.Artists)
                .Include(t => t.Publishers)
                .Include(t => t.Teams)
                .Include(t => t.Categories)
                .Include(t => t.Tags)
                .Include(t => t.Formats)
                .FirstOrDefaultAsync(t => t.Id == pendingTitleId, ct);

            if (pending == null)
                return ApprovalOutcome<RejectedTitle>.NotFound("Pending title not found");

            var rejected = new RejectedTitle
            {
                OriginalTitle = pending.OriginalTitle,
                EnglishTitle = pending.EnglishTitle,
                AlternativeNames = pending.AlternativeNames,
                ReleaseDate = pending.ReleaseDate,
                Description = pending.Description,
                StatusTitle = "Rejected",
                StatusTranslation = pending.StatusTranslation,
                Type = pending.Type,
                AgeRestriction = pending.AgeRestriction,
                CoverImagePath = pending.CoverImagePath,
                BackgroundImagePath = pending.BackgroundImagePath,
                ExternalLinksSerialized = pending.ExternalLinksSerialized,
                CreatedByUserId = pending.CreatedByUserId,
                CreatedAt = pending.CreatedAt,
                RejectedAt = DateTime.UtcNow,
                RejectionReason = reason ?? "No reason provided",
                Authors = pending.Authors,
                Artists = pending.Artists,
                Publishers = pending.Publishers,
                Teams = pending.Teams,
                Categories = pending.Categories,
                Tags = pending.Tags,
                Formats = pending.Formats
            };

            await ExecuteInTransactionAsync(async () =>
            {
                _db.Set<RejectedTitle>().Add(rejected);
                _db.Set<PendingTitle>().Remove(pending);
                await _db.SaveChangesAsync(ct);

                if (!string.IsNullOrEmpty(pending.CreatedByUserId))
                    await _trust.RecordRejectionAsync(pending.CreatedByUserId, TrustActionType.AddTitle);
            }, ct);

            _logger.LogInformation("Pending title rejected: {Title}, Reason: {Reason}",
                rejected.EnglishTitle, reason);
            return ApprovalOutcome<RejectedTitle>.Ok(rejected);
        }

        // ── Chapter (Add or Edit) ───────────────────────────────────────────

        public async Task<ApprovalOutcome<Chapter>> ApproveChapterAsync(
            int pendingChapterId, AppUser reviewer, CancellationToken ct = default)
        {
            var pending = await _db.PendingChapters
                .Include(pc => pc.Title)
                .Include(pc => pc.Team)
                .FirstOrDefaultAsync(pc => pc.Id == pendingChapterId, ct);

            if (pending == null)
                return ApprovalOutcome<Chapter>.NotFound("Pending chapter not found");

            if (!pending.TitleId.HasValue)
                return ApprovalOutcome<Chapter>.BadRequest(
                    "Cannot approve this chapter because its title is still pending approval");

            Chapter result = null!;
            ApprovalOutcome<Chapter>? earlyResult = null;
            await ExecuteInTransactionAsync(async () =>
            {
                // Load the original inside the transaction so a concurrent delete
                // between request start and mutation can't slip through.
                Chapter? original = null;
                if (pending.OriginalChapterId.HasValue)
                {
                    original = await _db.Chapters
                        .Include(c => c.Title)
                        .Include(c => c.Team)
                        .FirstOrDefaultAsync(c => c.Id == pending.OriginalChapterId.Value, ct);
                    if (original == null)
                    {
                        earlyResult = ApprovalOutcome<Chapter>.NotFound("Original chapter no longer exists");
                        return;
                    }
                }

                if (original != null)
                {
                    var oldName = original.Name;
                    original.Name = pending.Name;
                    original.VolumeNumber = pending.VolumeNumber;
                    original.ChapterNumber = pending.ChapterNumber;
                    original.TeamId = pending.TeamId;
                    original.Content = pending.Content;
                    original.LastUpdatedAt = DateTime.UtcNow;
                    original.UpdatedByUserId = reviewer.Id;

                    _db.PendingChapters.Remove(pending);

                    var pendingLog = await FindPendingChangeLogAsync(
                        pending.TitleId.Value, pending.UpdatedByUserId, "Edit Chapter", ct);

                    if (pendingLog != null)
                    {
                        MarkLogReviewed(pendingLog, ChangeLogStatus.Approved, reviewer.Id);
                    }
                    else
                    {
                        _db.TitleChangeLogs.Add(new TitleChangeLog
                        {
                            TitleId = pending.TitleId.Value,
                            UpdatedByUserId = pending.UpdatedByUserId,
                            ReviewedByUserId = reviewer.Id,
                            CreatedAt = pending.CreatedDate,
                            ReviewedAt = DateTime.UtcNow,
                            ChangeType = "Edit Chapter",
                            OldValue = $"Ch.{original.ChapterNumber} - {oldName}",
                            NewValue = $"Ch.{pending.ChapterNumber} - {pending.Name}",
                            AdminComment = "Approved by admin",
                            Status = ChangeLogStatus.Approved,
                        });
                    }

                    await _db.SaveChangesAsync(ct);
                    await _trust.RecordApprovalAsync(pending.UpdatedByUserId, TrustActionType.AddChapter);

                    result = await _db.Chapters
                        .Include(c => c.Title).Include(c => c.Team)
                        .FirstAsync(c => c.Id == original.Id, ct);

                    _logger.LogInformation("Chapter edit approved by {Admin}: Ch.{Num} for {Title}",
                        reviewer.UserName, original.ChapterNumber, pending.Title.OriginalTitle);
                }
                else
                {
                    var chapter = new Chapter
                    {
                        Name = pending.Name,
                        VolumeNumber = pending.VolumeNumber,
                        ChapterNumber = pending.ChapterNumber,
                        TitleId = pending.TitleId.Value,
                        TeamId = pending.TeamId,
                        CreatedDate = DateTime.UtcNow,
                        ReleaseDate = DateTime.UtcNow,
                        UpdatedByUserId = reviewer.Id,
                        Content = pending.Content
                    };

                    _db.Chapters.Add(chapter);
                    _db.PendingChapters.Remove(pending);
                    _db.TitleChangeLogs.Add(new TitleChangeLog
                    {
                        TitleId = pending.TitleId.Value,
                        UpdatedByUserId = pending.UpdatedByUserId,
                        ReviewedByUserId = reviewer.Id,
                        CreatedAt = pending.CreatedDate,
                        ReviewedAt = DateTime.UtcNow,
                        ChangeType = "Add Chapter",
                        OldValue = "",
                        NewValue = $"Ch.{pending.ChapterNumber} - {pending.Name}",
                        AdminComment = "Approved by admin",
                        Status = ChangeLogStatus.Approved,
                    });

                    await _db.SaveChangesAsync(ct);
                    await _trust.RecordApprovalAsync(pending.UpdatedByUserId, TrustActionType.AddChapter);

                    result = await _db.Chapters
                        .Include(c => c.Title).Include(c => c.Team)
                        .FirstAsync(c => c.Id == chapter.Id, ct);

                    _logger.LogInformation("Chapter approved by {UserName}: {Name} for {TitleName}",
                        reviewer.UserName, chapter.Name, pending.Title.OriginalTitle);
                }
            }, ct);

            return earlyResult ?? ApprovalOutcome<Chapter>.Ok(result);
        }

        public async Task<ApprovalOutcome<RejectedChapter>> RejectChapterAsync(
            int pendingChapterId, AppUser reviewer, string? reason, CancellationToken ct = default)
        {
            var pending = await _db.PendingChapters
                .Include(pc => pc.Title)
                .FirstOrDefaultAsync(pc => pc.Id == pendingChapterId, ct);

            if (pending == null)
                return ApprovalOutcome<RejectedChapter>.NotFound("Pending chapter not found");

            if (!pending.TitleId.HasValue)
                return ApprovalOutcome<RejectedChapter>.BadRequest(
                    "Cannot reject this chapter because its title is still pending approval");

            var rejected = new RejectedChapter
            {
                Name = pending.Name,
                VolumeNumber = pending.VolumeNumber,
                ChapterNumber = pending.ChapterNumber,
                TitleId = pending.TitleId.Value,
                TeamId = pending.TeamId,
                CreatedDate = DateTime.UtcNow,
                UpdatedByUserId = reviewer.Id,
                Content = pending.Content
            };

            await ExecuteInTransactionAsync(async () =>
            {
                _db.RejectedChapters.Add(rejected);

                var changeType = pending.OriginalChapterId.HasValue ? "Edit Chapter" : "Add Chapter";
                var pendingLog = await FindPendingChangeLogAsync(
                    pending.TitleId.Value, pending.UpdatedByUserId, changeType, ct);

                if (pendingLog != null)
                {
                    MarkLogReviewed(pendingLog, ChangeLogStatus.Rejected, reviewer.Id, reason);
                }
                else
                {
                    _db.TitleChangeLogs.Add(new TitleChangeLog
                    {
                        TitleId = pending.TitleId.Value,
                        UpdatedByUserId = pending.UpdatedByUserId,
                        ReviewedByUserId = reviewer.Id,
                        CreatedAt = pending.CreatedDate,
                        ReviewedAt = DateTime.UtcNow,
                        ChangeType = changeType,
                        OldValue = "",
                        NewValue = $"Ch.{pending.ChapterNumber} - {pending.Name}",
                        AdminComment = "Rejected by admin",
                        RejectionReason = reason ?? string.Empty,
                        Status = ChangeLogStatus.Rejected,
                    });
                }

                _db.PendingChapters.Remove(pending);
                await _db.SaveChangesAsync(ct);
                await _trust.RecordRejectionAsync(pending.UpdatedByUserId, TrustActionType.AddChapter);
            }, ct);

            _logger.LogInformation("Pending chapter rejected: Ch.{Num} of {Title}, Reason: {Reason}",
                pending.ChapterNumber, pending.Title?.OriginalTitle, reason);
            return ApprovalOutcome<RejectedChapter>.Ok(rejected);
        }

        // ── EditTitle (TitleChangeLog batch) ────────────────────────────────

        public async Task<ApprovalOutcome<int>> ApproveTitleEditsAsync(
            int titleId, AppUser admin, string? adminComment, CancellationToken ct = default)
        {
            var pendingChanges = await _db.TitleChangeLogs
                .Where(tc => tc.TitleId == titleId && tc.Status == ChangeLogStatus.Pending)
                .Include(tc => tc.Title).ThenInclude(t => t.Categories)
                .Include(tc => tc.Title).ThenInclude(t => t.Tags)
                .Include(tc => tc.Title).ThenInclude(t => t.Formats)
                .Include(tc => tc.Title).ThenInclude(t => t.Authors)
                .Include(tc => tc.Title).ThenInclude(t => t.Artists)
                .Include(tc => tc.Title).ThenInclude(t => t.Publishers)
                .Include(tc => tc.Title).ThenInclude(t => t.Teams)
                .ToListAsync(ct);

            if (pendingChanges.Count == 0)
                return ApprovalOutcome<int>.NotFound("No pending changes found for this title");

            var title = pendingChanges[0].Title;
            var appliedCount = 0;

            await ExecuteInTransactionAsync(async () =>
            {
                appliedCount = 0;
                foreach (var change in pendingChanges)
                {
                    await ApplyTitleChangeAsync(title, change, ct);

                    change.Status = ChangeLogStatus.Approved;
                    change.ReviewedByUserId = admin.Id;
                    change.ReviewedAt = DateTime.UtcNow;
                    change.AdminComment = adminComment ?? string.Empty;

                    _db.ApprovedTitleChanges.Add(new ApprovedTitleChange
                    {
                        TitleId = titleId,
                        UpdatedByUserId = change.UpdatedByUserId,
                        ReviewedByUserId = admin.Id,
                        CreatedAt = change.CreatedAt,
                        ApprovedAt = DateTime.UtcNow,
                        ChangeType = change.ChangeType,
                        OldValue = change.OldValue,
                        NewValue = change.NewValue,
                        AdminComment = adminComment ?? string.Empty,
                        IsAutoApproved = false
                    });
                    appliedCount++;
                }

                _db.Titles.Update(title);
                await _db.SaveChangesAsync(ct);

                foreach (var submitterId in pendingChanges.Select(c => c.UpdatedByUserId).Distinct())
                    await _trust.RecordApprovalAsync(submitterId, TrustActionType.EditTitle);
            }, ct);

            _logger.LogInformation("Approved {Count} changes for title {TitleId} by admin {AdminId}",
                appliedCount, titleId, admin.Id);
            return ApprovalOutcome<int>.Ok(appliedCount);
        }

        public async Task<ApprovalOutcome<int>> RejectTitleEditsAsync(
            int titleId, AppUser admin, string reason, string? adminComment, CancellationToken ct = default)
        {
            var pendingChanges = await _db.TitleChangeLogs
                .Where(tc => tc.TitleId == titleId && tc.Status == ChangeLogStatus.Pending)
                .ToListAsync(ct);

            if (pendingChanges.Count == 0)
                return ApprovalOutcome<int>.NotFound("No pending changes found for this title");

            await ExecuteInTransactionAsync(async () =>
            {
                foreach (var change in pendingChanges)
                {
                    change.Status = ChangeLogStatus.Rejected;
                    change.ReviewedByUserId = admin.Id;
                    change.ReviewedAt = DateTime.UtcNow;
                    change.RejectionReason = reason;
                    change.AdminComment = adminComment ?? string.Empty;

                    _db.RejectedTitleChanges.Add(new RejectedTitleChange
                    {
                        TitleId = titleId,
                        UpdatedByUserId = change.UpdatedByUserId,
                        ReviewedByUserId = admin.Id,
                        CreatedAt = change.CreatedAt,
                        RejectedAt = DateTime.UtcNow,
                        ChangeType = change.ChangeType,
                        OldValue = change.OldValue,
                        NewValue = change.NewValue,
                        AdminComment = adminComment ?? string.Empty,
                        RejectionReason = reason
                    });
                }

                await _db.SaveChangesAsync(ct);

                foreach (var submitterId in pendingChanges.Select(c => c.UpdatedByUserId).Distinct())
                    await _trust.RecordRejectionAsync(submitterId, TrustActionType.EditTitle);
            }, ct);

            _logger.LogInformation("Rejected {Count} changes for title {TitleId} by admin {AdminId}",
                pendingChanges.Count, titleId, admin.Id);
            return ApprovalOutcome<int>.Ok(pendingChanges.Count);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        // Wraps an operation in EF's execution strategy + an explicit transaction so
        // retry-on-failure (configured in Program.cs) doesn't tear up a half-committed
        // approval. Without ExecuteAsync, opening a transaction manually disables
        // retries — and any mid-flight SaveChanges failure leaves partial state.
        //
        // The operation lambda must be idempotent: on a transient SQL failure the
        // strategy may invoke it more than once. Mutations to the tracked DbContext
        // and to closure variables get re-run from scratch each time, which is fine
        // for the approve/reject flows here (they only touch DB state through `_db`).
        // Don't make external calls inside the lambda.
        private async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                await operation();
                await tx.CommitAsync(ct);
            });
        }

        private Task<TitleChangeLog?> FindPendingChangeLogAsync(
            int titleId, string updatedByUserId, string changeType, CancellationToken ct)
            => _db.TitleChangeLogs
                .Where(l => l.TitleId == titleId
                         && l.UpdatedByUserId == updatedByUserId
                         && l.ChangeType == changeType
                         && l.Status == ChangeLogStatus.Pending)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync(ct);

        private static void MarkLogReviewed(
            TitleChangeLog log, ChangeLogStatus status, string reviewerId, string? reason = null)
        {
            log.Status = status;
            log.ReviewedByUserId = reviewerId;
            log.ReviewedAt = DateTime.UtcNow;
            log.AdminComment = status == ChangeLogStatus.Approved ? "Approved by admin" : "Rejected by admin";
            if (status == ChangeLogStatus.Rejected)
                log.RejectionReason = reason ?? string.Empty;
        }

        // Per-field application of a single TitleChangeLog. Mirrors the controller switch
        // until #38 lands; once TitleChangeApplicator exists this delegates to it.
        private async Task ApplyTitleChangeAsync(Title title, TitleChangeLog change, CancellationToken ct)
        {
            switch (change.ChangeType)
            {
                case "Original Title": title.OriginalTitle = change.NewValue; return;
                case "English Title": title.EnglishTitle = change.NewValue; return;
                case "Description": title.Description = change.NewValue; return;
                case "Alternative Names": title.AlternativeNames = change.NewValue; return;
                case "Release Date": title.ReleaseDate = change.NewValue; return;
                case "Status": title.StatusTitle = change.NewValue; return;
                case "Translation Status": title.StatusTranslation = change.NewValue; return;
                case "Type":
                    if (Enum.TryParse<MangaType>(change.NewValue, out var mangaType))
                        title.Type = mangaType;
                    return;
                case "Age Restriction":
                    if (int.TryParse(change.NewValue, out var ageRestriction))
                        title.AgeRestriction = ageRestriction;
                    return;
                case "Cover Image": title.CoverImagePath = change.NewValue; return;
                case "Background Image": title.BackgroundImagePath = change.NewValue; return;
                case "Authors": await ReplaceManyAsync<Author>(title.Authors, change.NewValue, ct); return;
                case "Artists": await ReplaceManyAsync<Artist>(title.Artists, change.NewValue, ct); return;
                case "Publishers": await ReplaceManyAsync<Publisher>(title.Publishers, change.NewValue, ct); return;
                case "Teams": await ReplaceManyAsync<Team>(title.Teams, change.NewValue, ct); return;
                case "Categories": await ReplaceManyAsync<Category>(title.Categories, change.NewValue, ct); return;
                case "Tags": await ReplaceManyAsync<Tag>(title.Tags, change.NewValue, ct); return;
                case "Formats": await ReplaceManyAsync<Format>(title.Formats, change.NewValue, ct); return;
                case "External Links": title.ExternalLinksSerialized = change.NewValue; return;
            }
        }

        private async Task ReplaceManyAsync<TEntity>(
            ICollection<TEntity> destination, string csv, CancellationToken ct) where TEntity : class
        {
            var ids = csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToList();
            var entities = await _db.Set<TEntity>()
                .Where(e => ids.Contains(EF.Property<int>(e, "Id")))
                .ToListAsync(ct);
            destination.Clear();
            foreach (var entity in entities)
                destination.Add(entity);
        }
    }

}
