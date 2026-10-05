using System.Security.Claims;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.AI;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FallenFaction.Server.Controllers
{
    /// <summary>
    /// Admin control of the external-worker translation pipeline:
    /// worker keys, per-title profile + glossary, enqueueing raw chapters, job review.
    /// </summary>
    [ApiController]
    [Route("api/admin/translation-pipeline")]
    [Authorize(Roles = "Admin")]
    public class AdminTranslationPipelineController : ControllerBase
    {
        private readonly ITranslationPipelineService _pipeline;
        private readonly UserManager<AppUser> _userManager;

        public AdminTranslationPipelineController(ITranslationPipelineService pipeline, UserManager<AppUser> userManager)
        {
            _pipeline = pipeline;
            _userManager = userManager;
        }

        // ── Workers ───────────────────────────────────────────────────────────

        /// <summary>Creates a worker and returns its API key. The key is shown only once.</summary>
        [HttpPost("workers")]
        public async Task<ActionResult<CreatedWorkerDto>> CreateWorker([FromBody] CreateWorkerDto dto, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return Ok(await _pipeline.CreateWorkerAsync(dto.Name, userId, ct));
        }

        [HttpGet("workers")]
        public async Task<ActionResult<List<WorkerDto>>> ListWorkers(CancellationToken ct) =>
            Ok(await _pipeline.ListWorkersAsync(ct));

        /// <summary>Revokes the key and returns the worker's in-flight jobs to the queue.</summary>
        [HttpDelete("workers/{id:int}")]
        public async Task<IActionResult> DeactivateWorker(int id, CancellationToken ct) =>
            await _pipeline.DeactivateWorkerAsync(id, ct) ? NoContent() : NotFound();

        // ── Title profile & glossary ──────────────────────────────────────────

        [HttpGet("titles/{titleId:int}/profile")]
        public async Task<ActionResult<TranslationProfileDto>> GetProfile(int titleId, CancellationToken ct)
        {
            var profile = await _pipeline.GetProfileAsync(titleId, ct);
            return profile == null ? NotFound(new { message = "No translation profile for this title yet." }) : Ok(profile);
        }

        [HttpPut("titles/{titleId:int}/profile")]
        public async Task<ActionResult<TranslationProfileDto>> PutProfile(int titleId, [FromBody] TranslationProfileDto dto, CancellationToken ct) =>
            ToResult(await _pipeline.UpsertProfileAsync(titleId, dto, ct));

        [HttpGet("titles/{titleId:int}/glossary")]
        public async Task<ActionResult<List<GlossaryEntryDto>>> GetGlossary(int titleId, CancellationToken ct) =>
            Ok(await _pipeline.GetGlossaryAsync(titleId, ct));

        /// <summary>Adds or overwrites terms. Admin-written terms are locked: workers must follow them.</summary>
        [HttpPut("titles/{titleId:int}/glossary")]
        public async Task<ActionResult<int>> PutGlossary(int titleId, [FromBody] List<GlossaryTermDto> terms, CancellationToken ct) =>
            ToResult(await _pipeline.UpsertGlossaryAsync(titleId, terms, ct));

        [HttpDelete("titles/{titleId:int}/glossary/{entryId:int}")]
        public async Task<IActionResult> DeleteGlossaryEntry(int titleId, int entryId, CancellationToken ct) =>
            await _pipeline.DeleteGlossaryEntryAsync(titleId, entryId, ct) ? NoContent() : NotFound();

        // ── Jobs ──────────────────────────────────────────────────────────────

        /// <summary>Enqueues raw chapters for translation. Chapters already queued for this title are skipped.</summary>
        [HttpPost("titles/{titleId:int}/jobs")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<EnqueueResultDto>> Enqueue(int titleId, [FromBody] EnqueueJobsDto dto, CancellationToken ct) =>
            ToResult(await _pipeline.EnqueueAsync(titleId, dto, ct));

        [HttpGet("titles/{titleId:int}/status")]
        public async Task<ActionResult<TitlePipelineStatusDto>> TitleStatus(int titleId, CancellationToken ct)
        {
            var status = await _pipeline.GetTitleStatusAsync(titleId, ct);
            return status == null ? NotFound() : Ok(status);
        }

        [HttpGet("jobs")]
        public async Task<ActionResult<List<TranslationJobListItemDto>>> ListJobs(
            [FromQuery] int? titleId, [FromQuery] TranslationJobStatus? status, [FromQuery] int take = 100, CancellationToken ct = default) =>
            Ok(await _pipeline.ListJobsAsync(titleId, status, take, ct));

        [HttpGet("jobs/{id:int}")]
        public async Task<ActionResult<TranslationJobDetailDto>> GetJob(int id, CancellationToken ct)
        {
            var job = await _pipeline.GetJobAsync(id, ct);
            return job == null ? NotFound() : Ok(job);
        }

        /// <summary>Re-queues a Failed / NeedsReview / Published job with a fresh attempt budget.</summary>
        [HttpPost("jobs/{id:int}/retry")]
        public async Task<IActionResult> Retry(int id, CancellationToken ct)
        {
            var outcome = await _pipeline.RetryJobAsync(id, ct);
            return outcome.Success ? Ok(new { status = outcome.Value }) : ToResult(outcome).Result!;
        }

        public class PublishReviewedDto
        {
            public string? TranslatedTitle { get; set; }
            public string? Content { get; set; }
        }

        /// <summary>Publishes a NeedsReview job, optionally with the admin's corrected text.</summary>
        [HttpPost("jobs/{id:int}/publish")]
        public async Task<ActionResult<CompleteJobResultDto>> Publish(int id, [FromBody] PublishReviewedDto? dto, CancellationToken ct)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return Unauthorized();
            return ToResult(await _pipeline.PublishReviewedJobAsync(id, admin, dto?.TranslatedTitle, dto?.Content, ct));
        }

        private ActionResult<T> ToResult<T>(ApprovalOutcome<T> outcome) => outcome.ErrorKind switch
        {
            ApprovalErrorKind.None => Ok(outcome.Value),
            ApprovalErrorKind.NotFound => NotFound(new { message = outcome.ErrorMessage }),
            _ => BadRequest(new { message = outcome.ErrorMessage })
        };
    }
}
