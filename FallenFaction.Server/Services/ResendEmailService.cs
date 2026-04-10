using Resend;
using FallenFaction.Server.Services.Interfaces;

namespace FallenFaction.Server.Services
{
    public class ResendEmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _config;
        private readonly ILogger<ResendEmailService> _logger;

        private string FromAddress => _config["Resend:FromEmail"] ?? "noreply@fallenfaction.com";
        private string AdminEmail => _config["Resend:AdminEmail"] ?? "contact@fallenfaction.com";
        private string DmcaEmail => _config["Resend:DmcaEmail"] ?? "dmca@fallenfaction.com";
        private string SiteName => "FallenFaction";
        private string SiteUrl => _config["ConnectionStrings:FrontendBaseUrl"] ?? "https://fallenfaction.com";

        public ResendEmailService(IResend resend, IConfiguration config, ILogger<ResendEmailService> logger)
        {
            _resend = resend;
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink)
        {
            var html = $"""
                {BaseTemplate($"Confirm your {SiteName} account", $"""
                <p style="margin:0 0 20px;font-size:16px;color:#374151;">Hi <strong>{HtmlEncode(userName)}</strong>,</p>
                <p style="margin:0 0 24px;font-size:15px;color:#4b5563;line-height:1.6;">
                    Welcome to {SiteName}! Please confirm your email address to activate your account and start reading.
                </p>
                <div style="text-align:center;margin:32px 0;">
                    <a href="{confirmationLink}"
                       style="display:inline-block;background:#4f46e5;color:#ffffff;font-size:15px;font-weight:600;
                              padding:14px 32px;border-radius:8px;text-decoration:none;letter-spacing:0.02em;">
                        Confirm Email Address
                    </a>
                </div>
                <p style="margin:24px 0 8px;font-size:13px;color:#6b7280;line-height:1.5;">
                    Or copy and paste this link into your browser:
                </p>
                <p style="margin:0 0 24px;font-size:12px;color:#4f46e5;word-break:break-all;">
                    {confirmationLink}
                </p>
                <p style="margin:0;font-size:13px;color:#9ca3af;">
                    This link expires in 24 hours. If you didn't create an account, you can ignore this email.
                </p>
                """)}
                """;

            await SendAsync(toEmail, $"Confirm your {SiteName} account", html);
        }

        public async Task SendContactMessageAsync(string fromEmail, string subject, string message)
        {
            var isDmca = subject.Equals("copyright", StringComparison.OrdinalIgnoreCase);
            var toEmail = isDmca ? DmcaEmail : AdminEmail;
            var subjectLabel = GetSubjectLabel(subject);

            var html = $"""
                {BaseTemplate($"[{SiteName}] New {subjectLabel} message", $"""
                <p style="margin:0 0 16px;font-size:15px;color:#374151;">
                    A new <strong>{HtmlEncode(subjectLabel)}</strong> message was submitted via the contact form.
                </p>
                <table style="width:100%;border-collapse:collapse;margin:0 0 24px;">
                    <tr>
                        <td style="padding:10px 14px;background:#f3f4f6;border-radius:6px 0 0 0;
                                   font-size:13px;font-weight:600;color:#374151;width:100px;vertical-align:top;">From</td>
                        <td style="padding:10px 14px;background:#f9fafb;border-radius:0 6px 0 0;
                                   font-size:14px;color:#111827;">{HtmlEncode(fromEmail)}</td>
                    </tr>
                    <tr>
                        <td style="padding:10px 14px;background:#f3f4f6;
                                   font-size:13px;font-weight:600;color:#374151;vertical-align:top;">Subject</td>
                        <td style="padding:10px 14px;background:#f9fafb;
                                   font-size:14px;color:#111827;">{HtmlEncode(subjectLabel)}</td>
                    </tr>
                    <tr>
                        <td style="padding:10px 14px;background:#f3f4f6;border-radius:0 0 0 6px;
                                   font-size:13px;font-weight:600;color:#374151;vertical-align:top;">Message</td>
                        <td style="padding:10px 14px;background:#f9fafb;border-radius:0 0 6px 0;
                                   font-size:14px;color:#111827;white-space:pre-wrap;line-height:1.6;">{HtmlEncode(message)}</td>
                    </tr>
                </table>
                <p style="margin:0;font-size:12px;color:#9ca3af;">
                    To reply, send to: {HtmlEncode(fromEmail)}
                </p>
                """)}
                """;

            await SendAsync(toEmail, $"[{SiteName}] {subjectLabel} — {fromEmail}", html);
        }

        public async Task SendContactConfirmationAsync(string toEmail, string subject)
        {
            var subjectLabel = GetSubjectLabel(subject);

            var html = $"""
                {BaseTemplate("We received your message", $"""
                <p style="margin:0 0 20px;font-size:16px;color:#374151;">Thank you for contacting {SiteName}!</p>
                <p style="margin:0 0 16px;font-size:15px;color:#4b5563;line-height:1.6;">
                    We've received your <strong>{HtmlEncode(subjectLabel)}</strong> message and will get back to you as soon as possible.
                </p>
                <p style="margin:0 0 24px;font-size:15px;color:#4b5563;line-height:1.6;">
                    Typical response time is 1–3 business days. For urgent copyright matters, expect a reply within 48 hours.
                </p>
                <div style="background:#f3f4f6;border-radius:8px;padding:16px 20px;margin:0 0 24px;">
                    <p style="margin:0;font-size:13px;color:#6b7280;">
                        You can also reach us directly:<br>
                        General: <a href="mailto:{AdminEmail}" style="color:#4f46e5;">{AdminEmail}</a><br>
                        DMCA / Copyright: <a href="mailto:{DmcaEmail}" style="color:#4f46e5;">{DmcaEmail}</a>
                    </p>
                </div>
                <p style="margin:0;font-size:13px;color:#9ca3af;">
                    If you didn't submit this form, please ignore this email.
                </p>
                """)}
                """;

            await SendAsync(toEmail, $"[{SiteName}] We received your message", html);
        }

        public async Task SendPasswordResetAsync(string toEmail, string userName, string resetLink)
        {
            var html = $"""
                {BaseTemplate("Reset your password", $"""
                <p style="margin:0 0 20px;font-size:16px;color:#374151;">Hi <strong>{HtmlEncode(userName)}</strong>,</p>
                <p style="margin:0 0 24px;font-size:15px;color:#4b5563;line-height:1.6;">
                    We received a request to reset your {SiteName} password. Click the button below to create a new password.
                </p>
                <div style="text-align:center;margin:32px 0;">
                    <a href="{resetLink}"
                       style="display:inline-block;background:#4f46e5;color:#ffffff;font-size:15px;font-weight:600;
                              padding:14px 32px;border-radius:8px;text-decoration:none;letter-spacing:0.02em;">
                        Reset Password
                    </a>
                </div>
                <p style="margin:24px 0 8px;font-size:13px;color:#6b7280;line-height:1.5;">
                    Or copy this link into your browser:
                </p>
                <p style="margin:0 0 24px;font-size:12px;color:#4f46e5;word-break:break-all;">
                    {resetLink}
                </p>
                <p style="margin:0;font-size:13px;color:#9ca3af;">
                    This link expires in 1 hour. If you didn't request a password reset, you can safely ignore this email.
                </p>
                """)}
                """;

            await SendAsync(toEmail, $"Reset your {SiteName} password", html);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task SendAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var message = new EmailMessage
                {
                    From = FromAddress,
                    Subject = subject,
                    HtmlBody = htmlBody
                };
                message.To.Add(to);

                await _resend.EmailSendAsync(message);
                _logger.LogInformation("Email '{Subject}' sent to {To}", subject, to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email '{Subject}' to {To}", subject, to);
                throw;
            }
        }

        private string BaseTemplate(string title, string content) => $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width,initial-scale=1.0" />
              <title>{HtmlEncode(title)}</title>
            </head>
            <body style="margin:0;padding:0;background:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f1f5f9;padding:40px 16px;">
                <tr>
                  <td align="center">
                    <table width="560" cellpadding="0" cellspacing="0"
                           style="background:#ffffff;border-radius:12px;overflow:hidden;
                                  box-shadow:0 4px 24px rgba(0,0,0,0.08);max-width:560px;width:100%;">

                      <!-- Header -->
                      <tr>
                        <td style="background:linear-gradient(135deg,#312e81 0%,#4f46e5 100%);
                                   padding:32px 40px;text-align:center;">
                          <a href="{SiteUrl}" style="text-decoration:none;">
                            <span style="font-size:22px;font-weight:800;color:#ffffff;letter-spacing:0.05em;">
                              FALLEN<span style="color:#a5b4fc;">FACTION</span>
                            </span>
                          </a>
                        </td>
                      </tr>

                      <!-- Body -->
                      <tr>
                        <td style="padding:40px 40px 32px;">
                          <h1 style="margin:0 0 24px;font-size:20px;font-weight:700;color:#111827;">
                            {HtmlEncode(title)}
                          </h1>
                          {content}
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="background:#f8fafc;border-top:1px solid #e5e7eb;
                                   padding:20px 40px;text-align:center;">
                          <p style="margin:0 0 6px;font-size:12px;color:#6b7280;">
                            © {DateTime.UtcNow.Year} {SiteName}. All rights reserved.
                          </p>
                          <p style="margin:0;font-size:12px;color:#9ca3af;">
                            <a href="{SiteUrl}/privacy" style="color:#4f46e5;text-decoration:none;">Privacy Policy</a>
                            &nbsp;·&nbsp;
                            <a href="{SiteUrl}/terms" style="color:#4f46e5;text-decoration:none;">Terms of Service</a>
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        private static string GetSubjectLabel(string subject) => subject switch
        {
            "bug" => "Bug Report",
            "feature" => "Feature Request",
            "copyright" => "Copyright / DMCA",
            "account" => "Account Issue",
            _ => "General Inquiry"
        };

        private static string HtmlEncode(string value) =>
            System.Net.WebUtility.HtmlEncode(value);
    }
}
