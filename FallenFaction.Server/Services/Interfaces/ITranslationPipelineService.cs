using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.AI;

namespace FallenFaction.Server.Services.Interfaces
{
    // Owns the external-worker translation queue: enqueue raw chapters, hand them
    // out under a lease (strictly in chapter order per title), accept results,
    // gate them on QA, and publish them as Chapters with the AI lock rules applied.
    public interface ITranslationPipelineService
    {
        // ── Worker side ──────────────────────────────────────────────────────
        Task<TranslationWorker?> AuthenticateWorkerAsync(string apiKey, CancellationToken ct = default);

        Task<ClaimedJobDto?> ClaimNextAsync(int workerId, CancellationToken ct = default);

        // Returns the new lease expiry, or null if the worker no longer holds the job.
        Task<DateTime?> HeartbeatAsync(int workerId, int jobId, CancellationToken ct = default);

        Task<ApprovalOutcome<CompleteJobResultDto>> CompleteAsync(
            int workerId, int jobId, CompleteJobDto dto, CancellationToken ct = default);

        Task<ApprovalOutcome<string>> FailAsync(
            int workerId, int jobId, FailJobDto dto, CancellationToken ct = default);

        // ── Admin side ───────────────────────────────────────────────────────
        Task<CreatedWorkerDto> CreateWorkerAsync(string name, string createdByUserId, CancellationToken ct = default);
        Task<List<WorkerDto>> ListWorkersAsync(CancellationToken ct = default);
        Task<bool> DeactivateWorkerAsync(int workerId, CancellationToken ct = default);

        Task<ApprovalOutcome<EnqueueResultDto>> EnqueueAsync(int titleId, EnqueueJobsDto dto, CancellationToken ct = default);

        Task<TranslationProfileDto?> GetProfileAsync(int titleId, CancellationToken ct = default);
        Task<ApprovalOutcome<TranslationProfileDto>> UpsertProfileAsync(int titleId, TranslationProfileDto dto, CancellationToken ct = default);

        Task<List<GlossaryEntryDto>> GetGlossaryAsync(int titleId, CancellationToken ct = default);
        Task<ApprovalOutcome<int>> UpsertGlossaryAsync(int titleId, List<GlossaryTermDto> terms, CancellationToken ct = default);
        Task<bool> DeleteGlossaryEntryAsync(int titleId, int entryId, CancellationToken ct = default);

        Task<List<TranslationJobListItemDto>> ListJobsAsync(int? titleId, TranslationJobStatus? status, int take, CancellationToken ct = default);
        Task<TranslationJobDetailDto?> GetJobAsync(int jobId, CancellationToken ct = default);
        Task<TitlePipelineStatusDto?> GetTitleStatusAsync(int titleId, CancellationToken ct = default);

        Task<ApprovalOutcome<string>> RetryJobAsync(int jobId, CancellationToken ct = default);

        // Publishes a NeedsReview job, optionally with admin-edited text.
        Task<ApprovalOutcome<CompleteJobResultDto>> PublishReviewedJobAsync(
            int jobId, AppUser admin, string? editedTitle, string? editedContent, CancellationToken ct = default);
    }
}
