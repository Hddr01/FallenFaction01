using Microsoft.AspNetCore.Http;

namespace FallenFaction.Server.Middleware
{
    /// <summary>
    /// Rejects well-known OS/browser crawler probe paths (apple-app-site-association,
    /// .well-known/*, robots.txt, etc.) at the pipeline level before they reach MVC routing.
    /// Prevents these paths from hitting database-backed controllers and generating noise in logs.
    /// </summary>
    public class CrawlerFilterMiddleware
    {
        private static readonly HashSet<string> _exactPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/apple-app-site-association",
            "/robots.txt",
            "/favicon.ico",
            "/sitemap.xml",
            "/browserconfig.xml",
        };

        private readonly RequestDelegate _next;

        public CrawlerFilterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            if (_exactPaths.Contains(path) ||
                path.StartsWith("/.well-known/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }
}
