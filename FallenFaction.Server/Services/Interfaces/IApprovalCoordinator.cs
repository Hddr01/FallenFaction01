using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Services.Interfaces
{
    // One module owning the Pending → Published lifecycle: it wraps every
    // approve/reject in a retry-aware transaction, writes the matching audit
    // row, and bumps the user's trust counter — so individual controllers
    // shrink to "validate request → call module" and can't silently forget
    // the audit/trust step.
    public interface IApprovalCoordinator
    {
        // ── PendingTitle ↔ Title (AddTitle) ─────────────────────────────────
        Task<ApprovalOutcome<Title>> ApproveAddTitleAsync(
            int pendingTitleId, AppUser admin, CancellationToken ct = default);

        Task<ApprovalOutcome<RejectedTitle>> RejectAddTitleAsync(
            int pendingTitleId, AppUser admin, string? reason, CancellationToken ct = default);

        // ── PendingChapter ↔ Chapter (AddChapter / EditChapter) ─────────────
        // Single entry covers both new-chapter and edit branches; the
        // PendingChapter.OriginalChapterId field decides which one runs.
        Task<ApprovalOutcome<Chapter>> ApproveChapterAsync(
            int pendingChapterId, AppUser reviewer, CancellationToken ct = default);

        Task<ApprovalOutcome<RejectedChapter>> RejectChapterAsync(
            int pendingChapterId, AppUser reviewer, string? reason, CancellationToken ct = default);

        // ── TitleChangeLog batch ↔ Title (EditTitle) ────────────────────────
        Task<ApprovalOutcome<int>> ApproveTitleEditsAsync(
            int titleId, AppUser admin, string? adminComment, CancellationToken ct = default);

        Task<ApprovalOutcome<int>> RejectTitleEditsAsync(
            int titleId, AppUser admin, string reason, string? adminComment, CancellationToken ct = default);
    }

    // Discriminated result so callers can map cleanly to HTTP status codes
    // without the coordinator depending on Mvc types.
    public sealed record ApprovalOutcome<T>(
        bool Success,
        T? Value,
        string? ErrorMessage = null,
        ApprovalErrorKind ErrorKind = ApprovalErrorKind.None)
    {
        public static ApprovalOutcome<T> Ok(T value) => new(true, value);
        public static ApprovalOutcome<T> NotFound(string message) =>
            new(false, default, message, ApprovalErrorKind.NotFound);
        public static ApprovalOutcome<T> BadRequest(string message) =>
            new(false, default, message, ApprovalErrorKind.BadRequest);
    }

    public enum ApprovalErrorKind
    {
        None,
        NotFound,
        BadRequest
    }
}
