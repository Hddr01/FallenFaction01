using FallenFaction.Server.DTOs.AI;
using FallenFaction.Server.Services;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FallenFaction.Server.Controllers
{
    /// <summary>
    /// Endpoints called by external translation workers (translation-worker/ in the repo).
    /// Authenticated by X-Worker-Key, never by user JWTs.
    /// </summary>
    [ApiController]
    [Route("api/translation-worker")]
    [Authorize(AuthenticationSchemes = WorkerKeyAuthenticationHandler.SchemeName)]
    public class TranslationWorkerController : ControllerBase
    {
        private readonly ITranslationPipelineService _pipeline;

        public TranslationWorkerController(ITranslationPipelineService pipeline)
        {
            _pipeline = pipeline;
        }

        private int WorkerId => int.Parse(User.FindFirst(WorkerKeyAuthenticationHandler.WorkerIdClaim)!.Value);

        // ── POST /api/translation-worker/claim ────────────────────────────────
        /// <summary>Claims the next eligible job. 204 when the queue is empty.</summary>
        [HttpPost("claim")]
        public async Task<ActionResult<ClaimedJobDto>> Claim(CancellationToken ct)
        {
            var job = await _pipeline.ClaimNextAsync(WorkerId, ct);
            return job == null ? NoContent() : Ok(job);
        }

        // ── POST /api/translation-worker/jobs/{id}/heartbeat ──────────────────
        [HttpPost("jobs/{id:int}/heartbeat")]
        public async Task<IActionResult> Heartbeat(int id, CancellationToken ct)
        {
            var lease = await _pipeline.HeartbeatAsync(WorkerId, id, ct);
            return lease == null
                ? Conflict(new { message = "Lease lost — stop working on this job." })
                : Ok(new { leaseExpiresAt = lease });
        }

        // ── POST /api/translation-worker/jobs/{id}/complete ───────────────────
        [HttpPost("jobs/{id:int}/complete")]
        public async Task<ActionResult<CompleteJobResultDto>> Complete(int id, [FromBody] CompleteJobDto dto, CancellationToken ct)
        {
            var outcome = await _pipeline.CompleteAsync(WorkerId, id, dto, ct);
            return ToResult(outcome);
        }

        // ── POST /api/translation-worker/jobs/{id}/fail ───────────────────────
        [HttpPost("jobs/{id:int}/fail")]
        public async Task<IActionResult> Fail(int id, [FromBody] FailJobDto dto, CancellationToken ct)
        {
            var outcome = await _pipeline.FailAsync(WorkerId, id, dto, ct);
            return outcome.Success ? Ok(new { status = outcome.Value }) : ToResult(outcome).Result!;
        }

        private ActionResult<T> ToResult<T>(ApprovalOutcome<T> outcome) => outcome.ErrorKind switch
        {
            ApprovalErrorKind.None => Ok(outcome.Value),
            ApprovalErrorKind.NotFound => NotFound(new { message = outcome.ErrorMessage }),
            _ => Conflict(new { message = outcome.ErrorMessage })
        };
    }
}
