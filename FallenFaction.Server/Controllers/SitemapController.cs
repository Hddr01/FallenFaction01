using FallenFaction.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    public class SitemapController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private const string BaseUrl = "https://fallenfaction.com";

        public SitemapController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> GetSitemap()
        {
            var titles = await _db.Titles
                .Where(t => t.IsAvailable)
                .Select(t => new { t.Id, t.OriginalTitle, t.CreatedAt })
                .ToListAsync();

            var chapters = await _db.Chapters
                .Where(c => c.Title.IsAvailable && !c.IsAILocked)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.ChapterNumber,
                    c.VolumeNumber,
                    c.TeamId,
                    c.ReleaseDate,
                    TitleId = c.TitleId,
                    TitleOriginalTitle = c.Title.OriginalTitle,
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // Static pages
            AddUrl(sb, BaseUrl + "/", "weekly", "1.0");
            AddUrl(sb, BaseUrl + "/catalog", "daily", "0.9");

            // Title pages
            foreach (var title in titles)
            {
                var slug = BuildSlug(title.OriginalTitle, title.Id);
                AddUrl(sb, $"{BaseUrl}/{slug}", "weekly", "0.8",
                    title.CreatedAt.ToString("yyyy-MM-dd"));
            }

            // Chapter pages
            foreach (var ch in chapters)
            {
                var titleSlug = BuildSlug(ch.TitleOriginalTitle, ch.TitleId);
                var chapterSeg = Uri.EscapeDataString(
                    !string.IsNullOrWhiteSpace(ch.Name)
                        ? ch.Name.Trim()
                        : ch.ChapterNumber.ToString());
                var teamId = ch.TeamId ?? 0;
                AddUrl(sb, $"{BaseUrl}/{titleSlug}/chapter/{chapterSeg}/v{ch.VolumeNumber}/t{teamId}?cid={ch.Id}",
                    "monthly", "0.6", ch.ReleaseDate.ToString("yyyy-MM-dd"));
            }

            sb.AppendLine("</urlset>");

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        private static void AddUrl(StringBuilder sb, string loc, string changefreq, string priority, string? lastmod = null)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{SecurityElement.Escape(loc)}</loc>");
            if (lastmod != null)
                sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
            sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");
        }

        private static string BuildSlug(string originalTitle, int id)
        {
            var slug = Regex.Replace((originalTitle ?? "").ToLowerInvariant(), @"[^a-z0-9]+", "-")
                            .Trim('-');
            slug = Regex.Replace(slug, @"-{2,}", "-");
            return $"{(string.IsNullOrEmpty(slug) ? "title" : slug)}-{id}";
        }
    }
}
