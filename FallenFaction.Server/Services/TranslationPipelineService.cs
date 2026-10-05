using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.AI;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Services
{
    public sealed class TranslationPipelineService : ITranslationPipelineService
    {
        public const int MaxAttempts = 3;
        public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(20);

        // AI titles give their first N chapters away for free (same rule as AITeamSeeder).
        private const int FreeChapterCount = 50;
        private const int PreviousSummaryCount = 5;

        // Statuses of an EARLIER chapter that stop a later chapter of the same title from
        // being claimed. NeedsReview does not block: its summary and glossary terms already exist.
        private static readonly TranslationJobStatus[] BlockingStatuses =
        {
            TranslationJobStatus.Queued,
            TranslationJobStatus.Claimed,
            TranslationJobStatus.Failed
        };

        private readonly ApplicationDbContext _db;
        private readonly ILogger<TranslationPipelineService> _logger;

        public TranslationPipelineService(ApplicationDbContext db, ILogger<TranslationPipelineService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ══ Worker side ══════════════════════════════════════════════════════

        public async Task<TranslationWorker?> AuthenticateWorkerAsync(string apiKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            var hash = HashKey(apiKey);
            var worker = await _db.TranslationWorkers.AsNoTracking()
                .FirstOrDefaultAsync(w => w.KeyHash == hash && w.IsActive, ct);
            if (worker == null) return null;

            // Cheap "last seen" bookkeeping — at most one write per minute per worker
            var now = DateTime.UtcNow;
            var staleBefore = now.AddMinutes(-1);
            await _db.TranslationWorkers
                .Where(w => w.Id == worker.Id && (w.LastSeenAt == null || w.LastSeenAt < staleBefore))
                .ExecuteUpdateAsync(s => s.SetProperty(w => w.LastSeenAt, now), ct);

            return worker;
        }

        public async Task<ClaimedJobDto?> ClaimNextAsync(int workerId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // Leases that expired on their last allowed attempt become Failed instead of being re-handed out.
            await _db.TranslationJobs
                .Where(j => j.Status == TranslationJobStatus.Claimed && j.LeaseExpiresAt < now && j.Attempts >= MaxAttempts)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, TranslationJobStatus.Failed)
                    .SetProperty(j => j.ClaimedByWorkerId, (int?)null)
                    .SetProperty(j => j.LastError, "Worker lease expired on the final attempt.")
                    .SetProperty(j => j.UpdatedAt, now), ct);

            var candidateIds = await _db.TranslationJobs
                .Where(j => j.Status == TranslationJobStatus.Queued
                         || (j.Status == TranslationJobStatus.Claimed && j.LeaseExpiresAt < now))
                .Where(j => !_db.TranslationJobs.Any(k =>
                    k.TitleId == j.TitleId &&
                    BlockingStatuses.Contains(k.Status) &&
                    (k.VolumeNumber < j.VolumeNumber ||
                     (k.VolumeNumber == j.VolumeNumber && k.ChapterNumber < j.ChapterNumber))))
                .OrderBy(j => j.CreatedAt).ThenBy(j => j.Id)
                .Select(j => j.Id)
                .Take(5)
                .ToListAsync(ct);

            foreach (var id in candidateIds)
            {
                var leaseUntil = now.Add(LeaseDuration);

                // Atomic compare-and-set: only one worker can win a given job
                var won = await _db.TranslationJobs
                    .Where(j => j.Id == id &&
                                (j.Status == TranslationJobStatus.Queued ||
                                 (j.Status == TranslationJobStatus.Claimed && j.LeaseExpiresAt < now)))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(j => j.Status, TranslationJobStatus.Claimed)
                        .SetProperty(j => j.ClaimedByWorkerId, workerId)
                        .SetProperty(j => j.LeaseExpiresAt, leaseUntil)
                        .SetProperty(j => j.ClaimedAt, now)
                        .SetProperty(j => j.Attempts, j => j.Attempts + 1)
                        .SetProperty(j => j.UpdatedAt, now), ct);

                if (won == 1)
                {
                    _logger.LogInformation("Translation job {JobId} claimed by worker {WorkerId}.", id, workerId);
                    return await BuildClaimedJobAsync(id, ct);
                }
            }

            return null;
        }

        private async Task<ClaimedJobDto> BuildClaimedJobAsync(int jobId, CancellationToken ct)
        {
            var job = await _db.TranslationJobs.AsNoTracking()
                .Include(j => j.Title)
                .FirstAsync(j => j.Id == jobId, ct);

            var profile = await _db.TranslationTitleProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.TitleId == job.TitleId, ct);

            // Exact substring match beats embeddings for names: 林动 either appears or it doesn't.
            var glossary = (await _db.TranslationGlossaryEntries.AsNoTracking()
                    .Where(g => g.TitleId == job.TitleId)
                    .ToListAsync(ct))
                .Where(g => job.SourceText.Contains(g.SourceTerm, StringComparison.Ordinal)
                         || (job.SourceChapterTitle?.Contains(g.SourceTerm, StringComparison.Ordinal) ?? false))
                .OrderByDescending(g => g.SourceTerm.Length)
                .Select(g => new GlossaryTermDto
                {
                    SourceTerm = g.SourceTerm,
                    Translation = g.Translation,
                    Category = g.Category,
                    Notes = g.Notes,
                    IsLocked = g.IsLocked
                })
                .ToList();

            var previous = await _db.TranslationJobs.AsNoTracking()
                .Where(k => k.TitleId == job.TitleId && k.Summary != null &&
                            (k.VolumeNumber < job.VolumeNumber ||
                             (k.VolumeNumber == job.VolumeNumber && k.ChapterNumber < job.ChapterNumber)))
                .OrderByDescending(k => k.VolumeNumber).ThenByDescending(k => k.ChapterNumber)
                .Take(PreviousSummaryCount)
                .Select(k => new PreviousChapterSummaryDto
                {
                    VolumeNumber = k.VolumeNumber,
                    ChapterNumber = k.ChapterNumber,
                    Title = k.TranslatedTitle,
                    Summary = k.Summary!
                })
                .ToListAsync(ct);
            previous.Reverse();

            return new ClaimedJobDto
            {
                JobId = job.Id,
                TitleId = job.TitleId,
                TitleName = string.IsNullOrWhiteSpace(job.Title.EnglishTitle) ? job.Title.OriginalTitle : job.Title.EnglishTitle,
                OriginalTitle = job.Title.OriginalTitle,
                VolumeNumber = job.VolumeNumber,
                ChapterNumber = job.ChapterNumber,
                SourceChapterTitle = job.SourceChapterTitle,
                SourceText = job.SourceText,
                SourceLanguage = profile?.SourceLanguage ?? "zh",
                TargetLanguage = profile?.TargetLanguage ?? "en",
                StyleGuide = profile?.StyleGuide,
                Synopsis = profile?.Synopsis,
                LeaseExpiresAt = job.LeaseExpiresAt ?? DateTime.UtcNow.Add(LeaseDuration),
                Attempt = job.Attempts,
                Glossary = glossary,
                PreviousChapters = previous
            };
        }

        public async Task<DateTime?> HeartbeatAsync(int workerId, int jobId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var leaseUntil = now.Add(LeaseDuration);
            var rows = await _db.TranslationJobs
                .Where(j => j.Id == jobId && j.ClaimedByWorkerId == workerId && j.Status == TranslationJobStatus.Claimed)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.LeaseExpiresAt, leaseUntil)
                    .SetProperty(j => j.UpdatedAt, now), ct);
            return rows == 1 ? leaseUntil : null;
        }

        public async Task<ApprovalOutcome<CompleteJobResultDto>> CompleteAsync(
            int workerId, int jobId, CompleteJobDto dto, CancellationToken ct = default)
        {
            var content = NormalizeContent(dto.Content);
            if (content.Length == 0)
                return ApprovalOutcome<CompleteJobResultDto>.BadRequest("Translated content is empty.");

            var job = await _db.TranslationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job == null)
                return ApprovalOutcome<CompleteJobResultDto>.NotFound("Job not found.");
            if (job.Status != TranslationJobStatus.Claimed || job.ClaimedByWorkerId != workerId)
                return ApprovalOutcome<CompleteJobResultDto>.BadRequest("This worker no longer holds the job (lease lost or already completed).");

            var profile = await _db.TranslationTitleProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.TitleId == job.TitleId, ct);
            var minScore = profile?.MinQaScore ?? 7m;

            var result = new CompleteJobResultDto { JobId = job.Id };

            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                result.GlossaryTermsAdded = await AddProposedTermsAsync(job.TitleId, job.ChapterNumber, dto.NewGlossaryTerms, ct);

                var now = DateTime.UtcNow;
                job.TranslatedTitle = Truncate(dto.TranslatedTitle.Trim(), 255);
                job.TranslatedContent = content;
                job.Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : Truncate(dto.Summary.Trim(), 4000);
                job.QaScore = dto.QaScore;
                job.QaReport = dto.QaReport;
                job.ModelInfo = dto.ModelInfo;
                job.LastError = null;
                job.LeaseExpiresAt = null;
                job.CompletedAt = now;
                job.UpdatedAt = now;

                var needsReview = dto.QaFlagged
                               || (profile?.RequireReview ?? false)
                               || (dto.QaScore.HasValue && dto.QaScore.Value < minScore);

                if (needsReview)
                {
                    job.Status = TranslationJobStatus.NeedsReview;
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    var chapter = await PublishAsync(job, reviewerUserId: null, ct);
                    result.ChapterId = chapter.Id;
                }

                await tx.CommitAsync(ct);
                result.Status = job.Status.ToString();
            });

            _logger.LogInformation(
                "Translation job {JobId} completed by worker {WorkerId}: {Status} (QA {Score}, +{Terms} glossary terms).",
                job.Id, workerId, result.Status, dto.QaScore, result.GlossaryTermsAdded);

            return ApprovalOutcome<CompleteJobResultDto>.Ok(result);
        }

        public async Task<ApprovalOutcome<string>> FailAsync(
            int workerId, int jobId, FailJobDto dto, CancellationToken ct = default)
        {
            var job = await _db.TranslationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job == null)
                return ApprovalOutcome<string>.NotFound("Job not found.");
            if (job.Status != TranslationJobStatus.Claimed || job.ClaimedByWorkerId != workerId)
                return ApprovalOutcome<string>.BadRequest("This worker no longer holds the job.");

            if (!dto.CountAttempt && job.Attempts > 0)
                job.Attempts--; // the claim already counted it

            job.LastError = Truncate(dto.Error, 2000);
            job.Status = job.Attempts >= MaxAttempts ? TranslationJobStatus.Failed : TranslationJobStatus.Queued;
            job.ClaimedByWorkerId = null;
            job.LeaseExpiresAt = null;
            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning("Translation job {JobId} failed on worker {WorkerId} (attempt {Attempt}): {Error} → {Status}",
                job.Id, workerId, job.Attempts, dto.Error, job.Status);

            return ApprovalOutcome<string>.Ok(job.Status.ToString());
        }

        // ══ Publishing ═══════════════════════════════════════════════════════

        // Caller owns SaveChanges' surrounding transaction.
        private async Task<Chapter> PublishAsync(TranslationJob job, string? reviewerUserId, CancellationToken ct)
        {
            var title = await _db.Titles
                .Include(t => t.Teams)
                .FirstAsync(t => t.Id == job.TitleId, ct);

            var team = title.Teams.FirstOrDefault(t => t.GroupType == GroupType.AITranslation)
                    ?? title.Teams.FirstOrDefault(t => t.IsSystemTeam)
                    ?? await _db.Teams.FirstOrDefaultAsync(t => t.IsSystemTeam && t.GroupType == GroupType.AITranslation, ct);

            var authorId = reviewerUserId ?? team?.CreatorId
                ?? throw new InvalidOperationException("No AI/TL team found to own the published chapter.");

            var content = job.TranslatedContent ?? string.Empty;
            var name = string.IsNullOrWhiteSpace(job.TranslatedTitle) ? $"Chapter {job.ChapterNumber}" : job.TranslatedTitle;
            var now = DateTime.UtcNow;
            var language = await _db.TranslationTitleProfiles
                .Where(p => p.TitleId == job.TitleId)
                .Select(p => p.TargetLanguage)
                .FirstOrDefaultAsync(ct) ?? "en";

            var chapter = await _db.Chapters.FirstOrDefaultAsync(c =>
                c.TitleId == job.TitleId &&
                c.Language == language &&
                c.VolumeNumber == job.VolumeNumber &&
                c.ChapterNumber == job.ChapterNumber, ct);

            var isNew = chapter == null;
            if (chapter == null)
            {
                var chaptersBefore = await _db.Chapters.CountAsync(c =>
                    c.TitleId == job.TitleId &&
                    c.Language == language &&
                    (c.VolumeNumber < job.VolumeNumber ||
                     (c.VolumeNumber == job.VolumeNumber && c.ChapterNumber < job.ChapterNumber)), ct);

                chapter = new Chapter
                {
                    TitleId = job.TitleId,
                    TeamId = team?.Id,
                    VolumeNumber = job.VolumeNumber,
                    ChapterNumber = job.ChapterNumber,
                    Language = language,
                    CreatedDate = now,
                    ReleaseDate = now,
                    IsAILocked = title.TitleCategory == TitleCategory.AITranslation && chaptersBefore >= FreeChapterCount
                };
                _db.Chapters.Add(chapter);
            }

            // Re-translation of an existing chapter keeps its lock state (someone may have paid to unlock it)
            chapter.Name = name;
            chapter.Content = content;
            chapter.CharacterCount = content.Length;
            chapter.UpdatedByUserId = authorId;
            chapter.LastUpdatedAt = now;

            _db.TitleChangeLogs.Add(new TitleChangeLog
            {
                TitleId = job.TitleId,
                UpdatedByUserId = authorId,
                ReviewedByUserId = reviewerUserId,
                CreatedAt = job.CreatedAt,
                ReviewedAt = now,
                ChangeType = isNew ? "Add Chapter" : "Edit Chapter",
                OldValue = "",
                NewValue = $"Ch.{job.ChapterNumber} - {name}",
                AdminComment = reviewerUserId == null
                    ? $"AI translation pipeline (QA {job.QaScore?.ToString("0.0") ?? "n/a"})"
                    : "AI translation reviewed and published by admin",
                Status = reviewerUserId == null ? ChangeLogStatus.AutoApproved : ChangeLogStatus.Approved,
            });

            await _db.SaveChangesAsync(ct);

            job.Status = TranslationJobStatus.Published;
            job.ResultChapterId = chapter.Id;
            job.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);

            return chapter;
        }

        private async Task<int> AddProposedTermsAsync(int titleId, int chapterNumber, List<GlossaryTermDto> terms, CancellationToken ct)
        {
            if (terms.Count == 0) return 0;

            var existing = (await _db.TranslationGlossaryEntries
                    .Where(g => g.TitleId == titleId)
                    .Select(g => g.SourceTerm)
                    .ToListAsync(ct))
                .ToHashSet(GlossaryTermComparer);

            var added = 0;
            foreach (var term in terms)
            {
                var source = term.SourceTerm?.Trim() ?? "";
                var translation = term.Translation?.Trim() ?? "";
                if (source.Length == 0 || translation.Length == 0 || source.Length > 200) continue;
                if (!existing.Add(source)) continue; // never overwrite — first translation of a term sticks

                _db.TranslationGlossaryEntries.Add(new TranslationGlossaryEntry
                {
                    TitleId = titleId,
                    SourceTerm = source,
                    Translation = Truncate(translation, 300),
                    Category = Truncate(string.IsNullOrWhiteSpace(term.Category) ? "term" : term.Category.Trim().ToLowerInvariant(), 30),
                    Notes = term.Notes == null ? null : Truncate(term.Notes.Trim(), 500),
                    FirstSeenChapter = chapterNumber
                });
                added++;
            }

            if (added > 0) await _db.SaveChangesAsync(ct);
            return added;
        }

        // ══ Admin side ═══════════════════════════════════════════════════════

        public async Task<CreatedWorkerDto> CreateWorkerAsync(string name, string createdByUserId, CancellationToken ct = default)
        {
            var key = "ffw_" + Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var worker = new TranslationWorker
            {
                Name = name.Trim(),
                KeyHash = HashKey(key),
                KeyPrefix = key[..10],
                CreatedByUserId = createdByUserId
            };
            _db.TranslationWorkers.Add(worker);
            await _db.SaveChangesAsync(ct);

            return new CreatedWorkerDto
            {
                Id = worker.Id,
                Name = worker.Name,
                KeyPrefix = worker.KeyPrefix,
                IsActive = worker.IsActive,
                CreatedAt = worker.CreatedAt,
                ApiKey = key
            };
        }

        public Task<List<WorkerDto>> ListWorkersAsync(CancellationToken ct = default) =>
            _db.TranslationWorkers.AsNoTracking()
                .OrderBy(w => w.Id)
                .Select(w => new WorkerDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    KeyPrefix = w.KeyPrefix,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt,
                    LastSeenAt = w.LastSeenAt
                })
                .ToListAsync(ct);

        public async Task<bool> DeactivateWorkerAsync(int workerId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var rows = await _db.TranslationWorkers
                .Where(w => w.Id == workerId)
                .ExecuteUpdateAsync(s => s.SetProperty(w => w.IsActive, false), ct);
            if (rows == 0) return false;

            // Hand its in-flight jobs back to the queue right away
            await _db.TranslationJobs
                .Where(j => j.ClaimedByWorkerId == workerId && j.Status == TranslationJobStatus.Claimed)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, TranslationJobStatus.Queued)
                    .SetProperty(j => j.ClaimedByWorkerId, (int?)null)
                    .SetProperty(j => j.LeaseExpiresAt, (DateTime?)null)
                    .SetProperty(j => j.UpdatedAt, now), ct);
            return true;
        }

        public async Task<ApprovalOutcome<EnqueueResultDto>> EnqueueAsync(int titleId, EnqueueJobsDto dto, CancellationToken ct = default)
        {
            if (!await _db.Titles.AnyAsync(t => t.Id == titleId, ct))
                return ApprovalOutcome<EnqueueResultDto>.NotFound($"Title #{titleId} not found.");

            var duplicateInBatch = dto.Chapters
                .GroupBy(c => (c.VolumeNumber, c.ChapterNumber))
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateInBatch != null)
                return ApprovalOutcome<EnqueueResultDto>.BadRequest(
                    $"Vol.{duplicateInBatch.Key.VolumeNumber} Ch.{duplicateInBatch.Key.ChapterNumber} appears twice in the batch.");

            var existing = (await _db.TranslationJobs
                    .Where(j => j.TitleId == titleId)
                    .Select(j => new { j.VolumeNumber, j.ChapterNumber })
                    .ToListAsync(ct))
                .Select(j => (j.VolumeNumber, j.ChapterNumber))
                .ToHashSet();

            var result = new EnqueueResultDto();
            var jobs = new List<TranslationJob>();
            foreach (var c in dto.Chapters)
            {
                var text = NormalizeContent(c.SourceText);
                if (text.Length == 0 || existing.Contains((c.VolumeNumber, c.ChapterNumber)))
                {
                    result.SkippedExisting++;
                    continue;
                }

                jobs.Add(new TranslationJob
                {
                    TitleId = titleId,
                    VolumeNumber = c.VolumeNumber,
                    ChapterNumber = c.ChapterNumber,
                    SourceChapterTitle = c.SourceChapterTitle?.Trim(),
                    SourceText = text
                });
            }

            _db.TranslationJobs.AddRange(jobs);
            await _db.SaveChangesAsync(ct);

            result.Enqueued = jobs.Count;
            result.JobIds = jobs.Select(j => j.Id).ToList();
            return ApprovalOutcome<EnqueueResultDto>.Ok(result);
        }

        public Task<TranslationProfileDto?> GetProfileAsync(int titleId, CancellationToken ct = default) =>
            _db.TranslationTitleProfiles.AsNoTracking()
                .Where(p => p.TitleId == titleId)
                .Select(p => new TranslationProfileDto
                {
                    SourceLanguage = p.SourceLanguage,
                    TargetLanguage = p.TargetLanguage,
                    StyleGuide = p.StyleGuide,
                    Synopsis = p.Synopsis,
                    RequireReview = p.RequireReview,
                    MinQaScore = p.MinQaScore
                })
                .FirstOrDefaultAsync(ct);

        public async Task<ApprovalOutcome<TranslationProfileDto>> UpsertProfileAsync(int titleId, TranslationProfileDto dto, CancellationToken ct = default)
        {
            if (!await _db.Titles.AnyAsync(t => t.Id == titleId, ct))
                return ApprovalOutcome<TranslationProfileDto>.NotFound($"Title #{titleId} not found.");

            var profile = await _db.TranslationTitleProfiles.FirstOrDefaultAsync(p => p.TitleId == titleId, ct);
            if (profile == null)
            {
                profile = new TranslationTitleProfile { TitleId = titleId };
                _db.TranslationTitleProfiles.Add(profile);
            }

            profile.SourceLanguage = dto.SourceLanguage.Trim().ToLowerInvariant();
            profile.TargetLanguage = string.IsNullOrWhiteSpace(dto.TargetLanguage) ? "en" : dto.TargetLanguage.Trim().ToLowerInvariant();
            profile.StyleGuide = dto.StyleGuide;
            profile.Synopsis = dto.Synopsis;
            profile.RequireReview = dto.RequireReview;
            profile.MinQaScore = dto.MinQaScore;
            profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApprovalOutcome<TranslationProfileDto>.Ok(dto);
        }

        public Task<List<GlossaryEntryDto>> GetGlossaryAsync(int titleId, CancellationToken ct = default) =>
            _db.TranslationGlossaryEntries.AsNoTracking()
                .Where(g => g.TitleId == titleId)
                .OrderBy(g => g.Category).ThenBy(g => g.SourceTerm)
                .Select(g => new GlossaryEntryDto
                {
                    Id = g.Id,
                    SourceTerm = g.SourceTerm,
                    Translation = g.Translation,
                    Category = g.Category,
                    Notes = g.Notes,
                    IsLocked = g.IsLocked,
                    FirstSeenChapter = g.FirstSeenChapter,
                    UpdatedAt = g.UpdatedAt
                })
                .ToListAsync(ct);

        // Admin edits are authoritative: they overwrite and lock the entry.
        public async Task<ApprovalOutcome<int>> UpsertGlossaryAsync(int titleId, List<GlossaryTermDto> terms, CancellationToken ct = default)
        {
            if (!await _db.Titles.AnyAsync(t => t.Id == titleId, ct))
                return ApprovalOutcome<int>.NotFound($"Title #{titleId} not found.");

            var existing = await _db.TranslationGlossaryEntries
                .Where(g => g.TitleId == titleId)
                .ToDictionaryAsync(g => g.SourceTerm, GlossaryTermComparer, ct);

            var now = DateTime.UtcNow;
            var changed = 0;
            foreach (var term in terms)
            {
                var source = term.SourceTerm.Trim();
                if (source.Length == 0) continue;

                if (!existing.TryGetValue(source, out var entry))
                {
                    entry = new TranslationGlossaryEntry { TitleId = titleId, SourceTerm = source };
                    _db.TranslationGlossaryEntries.Add(entry);
                    existing[source] = entry;
                }

                entry.Translation = term.Translation.Trim();
                entry.Category = string.IsNullOrWhiteSpace(term.Category) ? "term" : term.Category.Trim().ToLowerInvariant();
                entry.Notes = term.Notes;
                entry.IsLocked = true;
                entry.UpdatedAt = now;
                changed++;
            }

            await _db.SaveChangesAsync(ct);
            return ApprovalOutcome<int>.Ok(changed);
        }

        public async Task<bool> DeleteGlossaryEntryAsync(int titleId, int entryId, CancellationToken ct = default) =>
            await _db.TranslationGlossaryEntries
                .Where(g => g.TitleId == titleId && g.Id == entryId)
                .ExecuteDeleteAsync(ct) == 1;

        public async Task<List<TranslationJobListItemDto>> ListJobsAsync(
            int? titleId, TranslationJobStatus? status, int take, CancellationToken ct = default)
        {
            var query = _db.TranslationJobs.AsNoTracking().AsQueryable();
            if (titleId.HasValue) query = query.Where(j => j.TitleId == titleId.Value);
            if (status.HasValue) query = query.Where(j => j.Status == status.Value);

            return await query
                .OrderBy(j => j.TitleId).ThenBy(j => j.VolumeNumber).ThenBy(j => j.ChapterNumber)
                .Take(Math.Clamp(take, 1, 500))
                .Select(ListItemProjection)
                .ToListAsync(ct);
        }

        public Task<TranslationJobDetailDto?> GetJobAsync(int jobId, CancellationToken ct = default) =>
            _db.TranslationJobs.AsNoTracking()
                .Where(j => j.Id == jobId)
                .Select(j => new TranslationJobDetailDto
                {
                    Id = j.Id,
                    TitleId = j.TitleId,
                    VolumeNumber = j.VolumeNumber,
                    ChapterNumber = j.ChapterNumber,
                    SourceChapterTitle = j.SourceChapterTitle,
                    Status = j.Status.ToString(),
                    Attempts = j.Attempts,
                    LastError = j.LastError,
                    WorkerName = j.ClaimedByWorker != null ? j.ClaimedByWorker.Name : null,
                    LeaseExpiresAt = j.LeaseExpiresAt,
                    QaScore = j.QaScore,
                    TranslatedTitle = j.TranslatedTitle,
                    ResultChapterId = j.ResultChapterId,
                    CreatedAt = j.CreatedAt,
                    CompletedAt = j.CompletedAt,
                    SourceText = j.SourceText,
                    TranslatedContent = j.TranslatedContent,
                    Summary = j.Summary,
                    QaReport = j.QaReport,
                    ModelInfo = j.ModelInfo
                })
                .FirstOrDefaultAsync(ct);

        public async Task<TitlePipelineStatusDto?> GetTitleStatusAsync(int titleId, CancellationToken ct = default)
        {
            var title = await _db.Titles.AsNoTracking()
                .Where(t => t.Id == titleId)
                .Select(t => new { t.Id, t.EnglishTitle, t.OriginalTitle })
                .FirstOrDefaultAsync(ct);
            if (title == null) return null;

            var counts = await _db.TranslationJobs.AsNoTracking()
                .Where(j => j.TitleId == titleId)
                .GroupBy(j => j.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var blockedBy = await _db.TranslationJobs.AsNoTracking()
                .Where(j => j.TitleId == titleId && j.Status == TranslationJobStatus.Failed)
                .OrderBy(j => j.VolumeNumber).ThenBy(j => j.ChapterNumber)
                .Select(ListItemProjection)
                .FirstOrDefaultAsync(ct);

            return new TitlePipelineStatusDto
            {
                TitleId = title.Id,
                TitleName = string.IsNullOrWhiteSpace(title.EnglishTitle) ? title.OriginalTitle : title.EnglishTitle,
                Counts = counts.ToDictionary(c => c.Key.ToString(), c => c.Count),
                BlockedBy = blockedBy
            };
        }

        public async Task<ApprovalOutcome<string>> RetryJobAsync(int jobId, CancellationToken ct = default)
        {
            var job = await _db.TranslationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job == null) return ApprovalOutcome<string>.NotFound("Job not found.");
            if (job.Status == TranslationJobStatus.Claimed)
                return ApprovalOutcome<string>.BadRequest("Job is currently being translated by a worker.");

            // A Published job can be re-translated: publishing again updates the existing chapter in place.
            job.Status = TranslationJobStatus.Queued;
            job.Attempts = 0;
            job.LastError = null;
            job.ClaimedByWorkerId = null;
            job.LeaseExpiresAt = null;
            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return ApprovalOutcome<string>.Ok(job.Status.ToString());
        }

        public async Task<ApprovalOutcome<CompleteJobResultDto>> PublishReviewedJobAsync(
            int jobId, AppUser admin, string? editedTitle, string? editedContent, CancellationToken ct = default)
        {
            var job = await _db.TranslationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job == null) return ApprovalOutcome<CompleteJobResultDto>.NotFound("Job not found.");
            if (job.Status != TranslationJobStatus.NeedsReview)
                return ApprovalOutcome<CompleteJobResultDto>.BadRequest($"Only NeedsReview jobs can be published (current: {job.Status}).");

            if (!string.IsNullOrWhiteSpace(editedTitle)) job.TranslatedTitle = Truncate(editedTitle.Trim(), 255);
            if (!string.IsNullOrWhiteSpace(editedContent)) job.TranslatedContent = NormalizeContent(editedContent);

            Chapter chapter = null!;
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                chapter = await PublishAsync(job, admin.Id, ct);
                await tx.CommitAsync(ct);
            });

            return ApprovalOutcome<CompleteJobResultDto>.Ok(new CompleteJobResultDto
            {
                JobId = job.Id,
                Status = job.Status.ToString(),
                ChapterId = chapter.Id
            });
        }

        // ══ Helpers ══════════════════════════════════════════════════════════

        private static readonly System.Linq.Expressions.Expression<Func<TranslationJob, TranslationJobListItemDto>> ListItemProjection =
            j => new TranslationJobListItemDto
            {
                Id = j.Id,
                TitleId = j.TitleId,
                VolumeNumber = j.VolumeNumber,
                ChapterNumber = j.ChapterNumber,
                SourceChapterTitle = j.SourceChapterTitle,
                Status = j.Status.ToString(),
                Attempts = j.Attempts,
                LastError = j.LastError,
                WorkerName = j.ClaimedByWorker != null ? j.ClaimedByWorker.Name : null,
                LeaseExpiresAt = j.LeaseExpiresAt,
                QaScore = j.QaScore,
                TranslatedTitle = j.TranslatedTitle,
                ResultChapterId = j.ResultChapterId,
                CreatedAt = j.CreatedAt,
                CompletedAt = j.CompletedAt
            };

        // Must treat terms as equal exactly when the DB's unique index does. The default SQL Server
        // collation (Latin1_General_CI_AS) ignores case, kana type and width, so ひらがな/カタカナ or
        // full-/half-width variants of one term would otherwise slip past an Ordinal check and then
        // fail the whole completion on the unique key.
        private static readonly StringComparer GlossaryTermComparer =
            CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(
                CompareOptions.IgnoreCase |
                CompareOptions.IgnoreKanaType |
                CompareOptions.IgnoreWidth);

        private static readonly Regex ExcessBlankLines = new(@"\n{3,}", RegexOptions.Compiled);

        // The reader splits paragraphs on blank lines and HTML-escapes the rest, so plain text with
        // \n\n between paragraphs is the canonical chapter format.
        internal static string NormalizeContent(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            normalized = string.Join('\n', normalized.Split('\n').Select(l => l.TrimEnd()));
            return ExcessBlankLines.Replace(normalized, "\n\n").Trim();
        }

        internal static string HashKey(string key) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max];
    }
}
