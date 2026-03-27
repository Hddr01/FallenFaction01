using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Data.SeedData;
using FallenFaction.Server.Mappings;
using FallenFaction.Server.Services;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container with explicit controller discovery
builder.Services.AddControllers(options =>
{
    // Add any global filters here if needed
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options =>
{
    // Handle circular references and improve JSON handling
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

    // CRITICAL: Increase max depth for deeply nested comment threads
    // Default is 32, but we support infinite comment nesting
    options.JsonSerializerOptions.MaxDepth = 128; // Supports up to 128 levels of nesting
});

// Explicitly add MVC services to ensure controller discovery
builder.Services.AddMvc();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<FallenFaction.Server.Services.Interfaces.ITrustService, FallenFaction.Server.Services.TrustService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHostedService<OnlineStatusCleanupService>();

// IMPROVED CORS Configuration - more comprehensive and secure
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        var allowedOrigins = new[] {
            "http://localhost:5173",
            "https://localhost:5173",
            "https://localhost:49217",  // Your Vue app HTTPS
            "http://localhost:49217",   // Your Vue app HTTP
            "https://localhost:7217",
            "http://localhost:7217",
            "http://localhost:5064",    // Your API port
            "https://localhost:5064"
        };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("*"); // Expose all headers
    });

    // More permissive development policy
    options.AddPolicy("DevelopmentCORS", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;

            try
            {
                var uri = new Uri(origin);
                // Allow all localhost and 127.0.0.1 origins in development
                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
            }
            catch
            {
                return false;
            }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("*");
    });
});

builder.Services.AddDbContext<FallenFaction.Server.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Email confirmation (disable for development)
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<FallenFaction.Server.Data.ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RequireExpirationTime = true
    };

    // Handle JWT events for better debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("JWT Token validated successfully");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge: {context.Error} - {context.ErrorDescription}");
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new { error = "Unauthorized", message = "Invalid or expired token" });
            return context.Response.WriteAsync(result);
        }
    };
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
});

// Add Authorization with detailed policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("AdminOrModerator", policy =>
        policy.RequireRole("Admin", "Moderator"));

    options.AddPolicy("AuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(AuthMappingProfile));

// Register custom services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FallenFaction API",
        Version = "v1",
        Description = "API for FallenFaction application"
    });


    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add configuration for static assets base URL
builder.Services.Configure<Dictionary<string, string>>(options =>
{
    options["StaticAssets:BaseUrl"] = builder.Configuration["StaticAssets:BaseUrl"] ?? "https://localhost:7217";
});

var app = builder.Build();
app.UseDeveloperExceptionPage();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FallenFaction API V1");
        c.DefaultModelsExpandDepth(-1);
    });

    // Use DevelopmentCORS in development for maximum compatibility
    app.UseCors("DevelopmentCORS");

    // Add request logging in development
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path} from {context.Connection.RemoteIpAddress}");
        await next();
        Console.WriteLine($"Response: {context.Response.StatusCode}");
    });
}
else
{
    app.UseHsts();
    app.UseCors("AllowVueApp");
}

// Force HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Authentication & Authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    await next();
});

// Serve static files (uploads, wwwroot) in production only.
// In dev, Vite serves its own assets and /uploads is proxied by Vite config.
if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

// Map controllers with detailed route debugging
app.MapControllers();

// Add a test endpoint to verify controller discovery
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/debug/controllers", () =>
    {
        var controllerActionDescriptor = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        return controllerActionDescriptor.ActionDescriptors.Items
            .Where(x => x.AttributeRouteInfo != null)
            .Select(x => new
            {
                Route = x.AttributeRouteInfo.Template,
                Controller = x.RouteValues["controller"],
                Action = x.RouteValues["action"],
                HttpMethods = string.Join(", ", x.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods ?? new[] { "ANY" })
            })
            .OrderBy(x => x.Route);
    }).WithOpenApi();
}

// Fallback: in development proxy unknown routes to Vite dev server;
// in production serve the built index.html from wwwroot.
if (app.Environment.IsDevelopment())
{
    app.MapFallback(async context =>
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/api") || path.StartsWith("/auth") || path.StartsWith("/uploads"))
        {
            context.Response.StatusCode = 404;
            return;
        }

        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var client = new HttpClient(handler);
        var viteUrl = $"https://localhost:49217{path}{context.Request.QueryString}";
        try
        {
            var response = await client.GetAsync(viteUrl);
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/html";
            var content = await response.Content.ReadAsStringAsync();
            await context.Response.WriteAsync(content);
        }
        catch
        {
            context.Response.StatusCode = 503;
            await context.Response.WriteAsync("Vite dev server not running. Run: cd fallenfaction.client && npm run dev");
        }
    });
}
else
{
    app.MapStaticAssets();
    app.MapFallbackToFile("/index.html");
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await PermissionSeeder.SeedPermissions(context);
}

// Seed default roles and create admin user if needed
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    var roles = new[] { "Admin", "Moderator", "User" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"Created role: {role}");
        }
    }

    // Create default admin user if none exists
    if (!userManager.Users.Any(u => u.Email == "admin@fallenfaction.com"))
    {
        var adminUser = new AppUser
        {
            UserName = "admin",
            Email = "admin@fallenfaction.com",
            EmailConfirmed = true,
            RegistrationDate = DateTime.UtcNow,
            LastActive = DateTime.UtcNow,
            IsActive = true,
            IsVerified = true,
            ProfilePicturePath = "https://localhost:7217/img/default-avatar.png"
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("Default admin user created: admin@fallenfaction.com / Admin123!");
        }
        else
        {
            Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}

Console.WriteLine("Application started successfully!");
app.Run();