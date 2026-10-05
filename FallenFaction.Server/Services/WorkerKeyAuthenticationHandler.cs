using System.Security.Claims;
using System.Text.Encodings.Web;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FallenFaction.Server.Services
{
    /// <summary>
    /// Authenticates translation workers by the X-Worker-Key header.
    /// Separate from the JWT default scheme: worker endpoints opt in with
    /// [Authorize(AuthenticationSchemes = WorkerKeyAuthenticationHandler.SchemeName)].
    /// </summary>
    public sealed class WorkerKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "WorkerKey";
        public const string HeaderName = "X-Worker-Key";
        public const string WorkerIdClaim = "worker_id";

        private readonly ITranslationPipelineService _pipeline;

        public WorkerKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ITranslationPipelineService pipeline)
            : base(options, logger, encoder)
        {
            _pipeline = pipeline;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var values))
                return AuthenticateResult.NoResult();

            var worker = await _pipeline.AuthenticateWorkerAsync(values.ToString(), Context.RequestAborted);
            if (worker == null)
                return AuthenticateResult.Fail("Invalid or inactive worker key.");

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(WorkerIdClaim, worker.Id.ToString()),
                new Claim(ClaimTypes.Name, worker.Name),
                new Claim(ClaimTypes.Role, "TranslationWorker")
            }, SchemeName);

            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
        }
    }
}
