using FallenFaction.Server.DTOs.Contact;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private static readonly HashSet<string> AllowedSubjects =
            new(StringComparer.OrdinalIgnoreCase) { "bug", "feature", "copyright", "account", "other" };

        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IEmailService emailService, ILogger<ContactController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Submits the contact / DMCA form. Sends a notification to the appropriate inbox
        /// and a confirmation email back to the sender.
        /// POST: api/contact
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("login")] // reuse 5/15-min limit — contact form abuse protection
        public async Task<IActionResult> Submit([FromBody] ContactFormDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Please fill in all required fields correctly." });

            if (!AllowedSubjects.Contains(dto.Subject))
                return BadRequest(new { message = "Invalid subject." });

            try
            {
                // Notify admin / DMCA inbox
                await _emailService.SendContactMessageAsync(dto.Email, dto.Subject, dto.Message);

                // Confirmation to sender (fire-and-forget — don't fail the request if this bounces)
                _ = _emailService.SendContactConfirmationAsync(dto.Email, dto.Subject)
                    .ContinueWith(t => _logger.LogWarning(t.Exception,
                        "Failed to send contact confirmation to {Email}", dto.Email),
                        TaskContinuationOptions.OnlyOnFaulted);

                _logger.LogInformation("Contact form submitted: subject={Subject}, from={Email}", dto.Subject, dto.Email);

                return Ok(new { success = true, message = "Message sent. We'll get back to you soon!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process contact form from {Email}", dto.Email);
                return StatusCode(500, new { message = "Failed to send your message. Please email us directly." });
            }
        }
    }
}
