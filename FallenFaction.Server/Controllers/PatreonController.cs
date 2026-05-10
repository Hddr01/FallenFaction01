using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Services.Interfaces;

namespace FallenFaction.Server.Controllers
{
    /// <summary>
    /// Handles Patreon OAuth account linking and pledge webhooks.
    ///
    /// Setup in appsettings.json:
    /// "Patreon": {
    ///   "ClientId":     "your_client_id",
    ///   "ClientSecret": "your_client_secret",
    ///   "RedirectUri":  "https://yoursite.com/api/patreon/callback",
    ///   "WebhookSecret":"your_webhook_secret",
    ///   "TierMapping": {
    ///     "Supporter":  5,
    ///     "Champion":   15,
    ///     "Patron":     30
    ///   }
    /// }
    /// </summary>
    [ApiController]
    [Route("api/patreon")]
    public class PatreonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<PatreonController> _logger;
        private readonly IMemoryCache _stateCache;
        private readonly ITicketWalletService _wallet;

        private static readonly HttpClient _http = new();
        private const string StateCachePrefix = "patreon_state_";

        public PatreonController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IConfiguration config,
            ILogger<PatreonController> logger,
            IMemoryCache stateCache,
            ITicketWalletService wallet)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
            _logger = logger;
            _stateCache = stateCache;
            _wallet = wallet;
        }

        // ── GET /api/patreon/link ────────────────────────────────────────────
        /// <summary>Returns the Patreon OAuth authorization URL for the frontend to redirect to.</summary>
        [HttpGet("link")]
        [Authorize]
        public IActionResult GetLinkUrl()
        {
            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var clientId = _config["Patreon:ClientId"];
            var redirectUri = Uri.EscapeDataString(_config["Patreon:RedirectUri"] ?? "");

            // Generate an unguessable state token and store userId → state for 10 minutes.
            // This prevents IDOR/CSRF: only the user who initiated the flow has a valid state.
            var state = Guid.NewGuid().ToString("N");
            _stateCache.Set(StateCachePrefix + state, userId, TimeSpan.FromMinutes(10));

            var url = $"https://www.patreon.com/oauth2/authorize" +
                      $"?response_type=code" +
                      $"&client_id={clientId}" +
                      $"&redirect_uri={redirectUri}" +
                      $"&scope=identity%20identity.memberships" +
                      $"&state={state}";

            return Ok(new { url });
        }

        // ── GET /api/patreon/callback ────────────────────────────────────────
        /// <summary>
        /// OAuth callback — exchanges code for tokens, stores on user, grants tickets.
        /// This endpoint is intentionally [AllowAnonymous]: Patreon redirects the browser here
        /// without a JWT Bearer header. CSRF/IDOR protection is provided by the random state token
        /// generated in GetLinkUrl and validated here.
        /// </summary>
        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(code))
                return BadRequest("Missing code or state.");

            // Validate and consume the one-time state token (prevents CSRF and IDOR).
            if (!_stateCache.TryGetValue(StateCachePrefix + state, out string? userId) || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Patreon callback: unknown or expired state token.");
                return BadRequest("Invalid or expired state. Please try linking again.");
            }
            _stateCache.Remove(StateCachePrefix + state); // single-use

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return BadRequest("User not found.");

            // Exchange code for tokens
            var tokenResponse = await ExchangeCodeAsync(code);
            if (tokenResponse == null)
                return StatusCode(502, "Failed to get Patreon tokens.");

            // Get Patreon identity
            var identity = await GetPatreonIdentityAsync(tokenResponse.Value.AccessToken);
            if (identity == null)
                return StatusCode(502, "Failed to get Patreon identity.");

            // Store tokens on user
            user.PatreonUserId        = identity.Value.PatreonUserId;
            user.PatreonAccessToken   = tokenResponse.Value.AccessToken;
            user.PatreonRefreshToken  = tokenResponse.Value.RefreshToken;
            user.PatreonLinkedAt      = DateTime.UtcNow;
            user.PatreonTierName      = identity.Value.TierName;
            user.PatreonMonthlyAmount = identity.Value.MonthlyAmount;

            // Grant initial Gold tickets for the current tier
            if (!string.IsNullOrEmpty(identity.Value.TierName))
                await GrantPatreonTicketsAsync(user, identity.Value.TierName, "Initial Patreon link grant");

            await _userManager.UpdateAsync(user);

            // Redirect back to the wallet page
            var frontendBase = _config["FrontendBaseUrl"]
                           ?? _config["ConnectionStrings:FrontendBaseUrl"]
                           ?? "http://localhost:5173";
            return Redirect($"{frontendBase}/profile/wallet?patreon=linked");
        }

        // ── DELETE /api/patreon/unlink ───────────────────────────────────────
        [HttpDelete("unlink")]
        [Authorize]
        public async Task<IActionResult> Unlink()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user   = await _userManager.FindByIdAsync(userId!);
            if (user == null) return Unauthorized();

            user.PatreonUserId       = null;
            user.PatreonAccessToken  = null;
            user.PatreonRefreshToken = null;
            user.PatreonLinkedAt     = null;
            user.PatreonTierName     = null;
            user.PatreonMonthlyAmount = 0;

            await _userManager.UpdateAsync(user);
            return Ok(new { message = "Patreon account unlinked." });
        }

        // ── GET /api/patreon/status ──────────────────────────────────────────
        [HttpGet("status")]
        [Authorize]
        public async Task<IActionResult> Status()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user   = await _context.Users
                .Select(u => new { u.Id, u.PatreonUserId, u.PatreonTierName, u.PatreonLinkedAt, u.PatreonMonthlyAmount })
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Unauthorized();

            return Ok(new
            {
                linked       = user.PatreonUserId != null,
                tierName     = user.PatreonTierName,
                linkedAt     = user.PatreonLinkedAt,
                monthlyAmount = user.PatreonMonthlyAmount
            });
        }

        // ── POST /api/patreon/webhook ────────────────────────────────────────
        /// <summary>
        /// Receives Patreon webhooks for pledge create/update/delete.
        /// Configure this URL in your Patreon creator portal.
        /// Events handled: members:pledge:create, members:pledge:update, members:pledge:delete
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            // Verify signature
            var secret    = _config["Patreon:WebhookSecret"] ?? "";
            var signature = Request.Headers["X-Patreon-Signature"].FirstOrDefault() ?? "";
            var eventType = Request.Headers["X-Patreon-Event"].FirstOrDefault() ?? "";

            using var bodyReader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await bodyReader.ReadToEndAsync();

            if (!VerifyWebhookSignature(body, signature, secret))
            {
                _logger.LogWarning("Patreon webhook signature mismatch.");
                return Unauthorized();
            }

            _logger.LogInformation("Patreon webhook: {Event}", eventType);

            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var data))
                    return Ok(); // Malformed or test payload — ignore

                // Patreon user id is under relationships.user.data.id
                string? patreonUserId = null;
                if (data.TryGetProperty("relationships", out var relationships) &&
                    relationships.TryGetProperty("user", out var userRel) &&
                    userRel.TryGetProperty("data", out var userData) &&
                    userData.ValueKind != JsonValueKind.Null &&
                    userData.TryGetProperty("id", out var userIdEl))
                {
                    patreonUserId = userIdEl.GetString();
                }

                if (string.IsNullOrEmpty(patreonUserId))
                    return Ok(); // Ignore unknown

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PatreonUserId == patreonUserId);

                if (user == null)
                {
                    _logger.LogInformation("Patreon webhook: no linked user for Patreon id {Id}", patreonUserId);
                    return Ok();
                }

                switch (eventType)
                {
                    case "members:pledge:create":
                    case "members:pledge:update":
                    {
                        // Extract tier title from included array
                        var tierName = ExtractTierName(doc.RootElement);
                        var amountCents = data.TryGetProperty("attributes", out var attrs) &&
                                          attrs.TryGetProperty("currently_entitled_amount_cents", out var amountEl)
                            ? amountEl.GetInt32()
                            : 0;

                        user.PatreonTierName     = tierName;
                        user.PatreonMonthlyAmount = amountCents / 100m;

                        await GrantPatreonTicketsAsync(user, tierName, $"Patreon {eventType}");
                        await _context.SaveChangesAsync();
                        break;
                    }

                    case "members:pledge:delete":
                        user.PatreonTierName     = null;
                        user.PatreonMonthlyAmount = 0;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Patreon pledge deleted for user {UserId}", user.Id);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Patreon webhook.");
                return StatusCode(500);
            }

            return Ok();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task GrantPatreonTicketsAsync(AppUser user, string tierName, string description)
        {
            var tierMapping = _config.GetSection("Patreon:TierMapping")
                .GetChildren()
                .ToDictionary(x => x.Key, x => decimal.Parse(x.Value ?? "0"));

            // Find the matching tier (case-insensitive prefix match)
            var goldAmount = tierMapping
                .Where(kv => tierName.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Value)
                .FirstOrDefault();

            if (goldAmount <= 0)
            {
                _logger.LogDebug("No tier mapping found for '{Tier}' — no tickets granted.", tierName);
                return;
            }

            await _wallet.CreditAsync(
                user.Id,
                TicketType.Gold,
                goldAmount,
                TicketTransactionType.PatreonGrant,
                $"{description} — {tierName} tier",
                patreonTierName: tierName);

            _logger.LogInformation("Granted {Gold} Gold tickets to {UserId} via Patreon tier '{Tier}'.",
                goldAmount, user.Id, tierName);
        }

        private async Task<(string AccessToken, string RefreshToken)?> ExchangeCodeAsync(string code)
        {
            var form = new Dictionary<string, string>
            {
                ["code"]          = code,
                ["grant_type"]    = "authorization_code",
                ["client_id"]     = _config["Patreon:ClientId"] ?? "",
                ["client_secret"] = _config["Patreon:ClientSecret"] ?? "",
                ["redirect_uri"]  = _config["Patreon:RedirectUri"] ?? ""
            };

            try
            {
                var resp = await _http.PostAsync(
                    "https://www.patreon.com/api/oauth2/token",
                    new FormUrlEncodedContent(form));

                if (!resp.IsSuccessStatusCode) return null;

                var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (!json.RootElement.TryGetProperty("access_token", out var atProp) ||
                    !json.RootElement.TryGetProperty("refresh_token", out var rtProp))
                    return null;
                var at = atProp.GetString() ?? "";
                var rt = rtProp.GetString() ?? "";
                return (at, rt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging Patreon code.");
                return null;
            }
        }

        private async Task<(string PatreonUserId, string? TierName, decimal MonthlyAmount)?> GetPatreonIdentityAsync(string accessToken)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    "https://www.patreon.com/api/oauth2/v2/identity" +
                    "?fields[user]=email,full_name" +
                    "&include=memberships.currently_entitled_tiers" +
                    "&fields[member]=currently_entitled_amount_cents" +
                    "&fields[tier]=title");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;

                var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (!json.RootElement.TryGetProperty("data", out var dataEl) ||
                    !dataEl.TryGetProperty("id", out var idEl))
                    return null;
                var userId = idEl.GetString() ?? "";
                var tierName = ExtractTierName(json.RootElement);
                var amount   = 0m;

                return (userId, tierName, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Patreon identity.");
                return null;
            }
        }

        private static string? ExtractTierName(JsonElement root)
        {
            if (!root.TryGetProperty("included", out var included) ||
                included.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var item in included.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var t) && t.GetString() == "tier" &&
                    item.TryGetProperty("attributes", out var attrs) &&
                    attrs.TryGetProperty("title", out var titleEl))
                    return titleEl.GetString();
            }
            return null;
        }

        private static bool VerifyWebhookSignature(string body, string signature, string secret)
        {
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("Patreon webhook secret is not configured. Set the Patreon__WebhookSecret environment variable.");
            using var hmac = new System.Security.Cryptography.HMACMD5(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            var signatureLower = signature.ToLowerInvariant();
            // Use constant-time comparison to prevent timing attacks.
            var computedBytes = Encoding.UTF8.GetBytes(computed);
            var signatureBytes = Encoding.UTF8.GetBytes(signatureLower);
            return computedBytes.Length == signatureBytes.Length &&
                   System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(computedBytes, signatureBytes);
        }
    }
}
