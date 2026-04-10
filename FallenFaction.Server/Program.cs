using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Data.SeedData;
using FallenFaction.Server.Mappings;
using FallenFaction.Server.Services;
using FallenFaction.Server.Services.Interfaces;
using Resend;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Limit request body size at the transport layer to prevent oversized payloads.
// Set to 10 MB to accommodate banner image uploads (UserProfileController allows up to 10 MB).
// Chapter content (~100k chars) is at most ~300 KB UTF-8, well within this limit.
// Per-endpoint size enforcement (avatars: 5 MB, banners: 10 MB, images: 5 MB) is
// applied inside each action for a user-friendly error response.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB global
});

// ── Sentry ────────────────────────────────────────────────────────────────────
builder.WebHost.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"];
    o.TracesSampleRate = 1.0;
    o.EnableLogs = false;
    o.SendDefaultPii = false;
    o.Debug = false;
});

#region Controllers
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
    // Allow nullable [FromBody] parameters to receive an empty or absent body.
    // Without this, Kestrel throws BadHttpRequestException("Unexpected end of request content")
    // on mobile clients that send Content-Length but drop the connection before the body arrives.
    options.AllowEmptyInputInBodyModelBinding = true;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

    options.JsonSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;

    options.JsonSerializerOptions.MaxDepth = 128;
});
#endregion

#region DataProtection
// Persist encryption keys to a Docker volume so they survive container restarts.
// Without this, ASP.NET generates new keys on every restart, invalidating all
// existing auth cookies / JWT validation material, and logs a noisy warning.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"))
    .SetApplicationName("FallenFaction");
#endregion

#region Services
builder.Services.AddResponseCaching();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITrustService, TrustService>();

builder.Services.AddAutoMapper(typeof(AuthMappingProfile));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ── Resend email ───────────────────────────────────────────────────────────
builder.Services.Configure<ResendClientOptions>(options =>
{
    var apiKey = builder.Configuration["Resend:ApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
        Console.WriteLine("[WARNING] Resend:ApiKey is not configured — emails will fail at send time. Set the Resend__ApiKey environment variable.");
    options.ApiToken = apiKey ?? string.Empty;
});
builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddScoped<IEmailService, ResendEmailService>();

builder.Services.AddHostedService<OnlineStatusCleanupService>();
builder.Services.AddHostedService<SilverTicketExpiryService>();
builder.Services.AddHostedService<AutoReleaseService>();
#endregion

#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:49217",
                "https://localhost:49217",
                "https://fallenfaction.com",
                "http://fallenfaction.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("DevelopmentCORS", policy =>
    {
        policy.SetIsOriginAllowed(o =>
        {
            if (string.IsNullOrEmpty(o)) return false;
            var uri = new Uri(o);
            return uri.Host == "localhost" || uri.Host == "127.0.0.1";
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
#endregion

#region DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 1;
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";
    options.SignIn.RequireConfirmedEmail = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
#endregion

#region JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"];
if (string.IsNullOrWhiteSpace(secret))
    throw new InvalidOperationException("JWT Secret is missing or empty. Set the JwtSettings__Secret environment variable.");

var key = Encoding.UTF8.GetBytes(secret);

// AddIdentity (above) overrides DefaultAuthenticateScheme to its cookie scheme.
// Explicitly re-set it here so JWT is used for [Authorize] on all controllers.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
#endregion

builder.Services.AddAuthorization();

#region Rate Limiting
// Reads X-Forwarded-For directly so the real client IP is used even before
// UseForwardedHeaders middleware has run (rate limiting evaluates at routing time).
static string GetClientIp(HttpContext context)
{
    if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
    {
        var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (ips.Length > 0 && !string.IsNullOrWhiteSpace(ips[0]))
            return ips[0].Trim();
    }
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

builder.Services.AddRateLimiter(options =>
{
    // Global limiter: applied to every request — general abuse protection
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = GetClientIp(context);
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // Login policy: strict per-IP limit for authentication endpoints
    options.AddPolicy("login", context =>
    {
        var ip = GetClientIp(context);
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // Ticket unlock policy: prevent rapid unlock spam (10 per minute per IP)
    options.AddPolicy("ticket-unlock", context =>
    {
        var ip = GetClientIp(context);
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // Comment creation policy: prevent comment spam (20 per minute per IP)
    options.AddPolicy("comment-create", context =>
    {
        var ip = GetClientIp(context);
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"Too many requests. Please try again later.\"}",
            cancellationToken: token);
    };
});
#endregion

builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

#region MIGRATION (🔥 IMPORTANT FIX)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
#endregion

#region SEED DATA (AFTER MIGRATION ONLY)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    await PermissionSeeder.SeedPermissions(db);
    await AITeamSeeder.SeedAsync(db, userManager);

    // Grandfather all pre-existing users as email-confirmed so they aren't locked out
    // when RequireConfirmedEmail becomes enforced in LoginAsync.
    var unconfirmed = userManager.Users.Where(u => !u.EmailConfirmed).ToList();
    foreach (var u in unconfirmed)
    {
        u.EmailConfirmed = true;
        await userManager.UpdateAsync(u);
    }
    if (unconfirmed.Count > 0)
        Console.WriteLine($"[Startup] Grandfathered {unconfirmed.Count} existing user(s) as email-confirmed.");

    string[] roles = { "Admin", "Moderator", "User" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    if (!userManager.Users.Any(u => u.Email == "admin@fallenfaction.com"))
    {
        var admin = new AppUser
        {
            UserName = "admin",
            Email = "admin@fallenfaction.com",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, "REDACTED");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
#endregion

#region PIPELINE

// Must be first so the rest of the pipeline sees the correct scheme/host/IP
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHsts();
app.UseRouting();
app.UseResponseCaching();
app.UseSentryTracing();  // ← traces every request

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' https:; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'");
    await next();
});

// CORS must be between UseRouting() and UseAuthentication() for endpoint routing
if (app.Environment.IsDevelopment())
    app.UseCors("DevelopmentCORS");
else
    app.UseCors("AllowVueApp");

// Rate limiter runs after CORS so preflight (OPTIONS) requests are not counted
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/llms.txt", () => Results.Text("""
# Fallen Faction

## About
Fallen Faction (https://fallenfaction.com) is a free web platform for reading
translated and original web novels, light novels, and short stories. It hosts
titles across genres including fantasy, cultivation (Wuxia, Xianxia, Xuanhuan),
romance, and classic fiction.

## What makes it different
- Clean, distraction-free reading experience
- Supports translated, original, fan-fiction, and AI-translated titles
- Chapter-by-chapter reading with volume organization
- Community features: ratings, comments, bookmarks, and reading progress tracking
- Team-based translation groups managing their own titles

## Content types
- Web Novels, Light Novels, Short Stories
- Wuxia, Xianxia, Xuanhuan (Chinese cultivation genres)
- Classic Fiction, Fan Fiction
- AI-assisted translations (ticket-gated)

## URL
https://fallenfaction.com

## Sitemap
https://fallenfaction.com/sitemap.xml
""", "text/plain"));

app.MapFallbackToFile("index.html");

#endregion

app.Run();