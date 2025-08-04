// Controllers/TitlesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.DTOs.Title;
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TitlesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TitlesController> _logger;
        private readonly Random _random;

        public TitlesController(ApplicationDbContext context, ILogger<TitlesController> logger)
        {
            _context = context;
            _logger = logger;
            _random = new Random();
        }

        /// <summary>
        /// Get debug info about database state
        /// GET: api/Titles/Debug
        /// </summary>
        [HttpGet("Debug")]
        public async Task<ActionResult> GetDebugInfo()
        {
            try
            {
                var titleCount = await _context.Titles.CountAsync();
                var availableTitleCount = await _context.Titles.CountAsync(t => t.IsAvailable);
                var userCount = await _context.Users.CountAsync();
                var teamCount = await _context.Teams.CountAsync();
                var chapterCount = await _context.Chapters.CountAsync();
                var chapterViewCount = await _context.ChapterViews.CountAsync();
                var ratingCount = await _context.Ratings.CountAsync();
                var bookmarkCount = await _context.Bookmarks.CountAsync();
                var bookmarkFolderCount = await _context.BookmarkFolders.CountAsync();

                var sampleTitles = await _context.Titles
                    .Include(t => t.Chapters)
                    .Include(t => t.Ratings)
                    .Include(t => t.Bookmarks)
                    .Take(3)
                    .Select(t => new {
                        t.Id,
                        t.OriginalTitle,
                        t.EnglishTitle,
                        t.IsAvailable,
                        t.ReleaseDate,
                        ChapterCount = t.Chapters.Count(),
                        LatestChapter = t.Chapters.Any() ? t.Chapters.Max(c => c.ChapterNumber) : 0,
                        LastUpdate = t.Chapters.Any() ? t.Chapters.Max(c => c.ReleaseDate) : (DateTime?)null,
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => (double)r.Value) : 0.0,
                        BookmarkCount = t.Bookmarks.Count(),
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalTitles = titleCount,
                    AvailableTitles = availableTitleCount,
                    TotalUsers = userCount,
                    TotalTeams = teamCount,
                    TotalChapters = chapterCount,
                    TotalChapterViews = chapterViewCount,
                    TotalRatings = ratingCount,
                    TotalBookmarks = bookmarkCount,
                    TotalBookmarkFolders = bookmarkFolderCount,
                    SampleTitles = sampleTitles,
                    DatabaseConnected = true,
                    ModelInfo = new
                    {
                        RatingScale = "1-10 (integer)",
                        BookmarkSystem = "Folder-based with LastReadChapter tracking",
                        ChapterViewTracking = "Per-user view tracking with IP and UserAgent",
                        CommentSystem = "Nested comments with reactions"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debug info");
                return Ok(new
                {
                    Error = ex.Message,
                    DatabaseConnected = false
                });
            }
        }

        /// <summary>
        /// Get featured titles for homepage
        /// GET: api/Titles/Featured
        /// </summary>
        [HttpGet("Featured")]
        public async Task<ActionResult<IEnumerable<TitleFeaturedDto>>> GetFeaturedTitles()
        {
            try
            {
                _logger.LogInformation("Fetching featured titles");

                var featuredTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .Include(t => t.Chapters)
                    .Select(t => new TitleFeaturedDto
                    {
                        Id = t.Id,
                        OriginalTitle = t.OriginalTitle ?? "Unknown Title",
                        EnglishTitle = t.EnglishTitle ?? t.OriginalTitle ?? "Unknown Title",
                        CoverImagePath = !string.IsNullOrEmpty(t.CoverImagePath) ? t.CoverImagePath : "/img/logo.png",
                        Type = t.Type,
                        LatestChapter = t.Chapters.Any() ?
                            t.Chapters.Max(c => c.ChapterNumber).ToString() :
                            "No chapters",
                        Description = t.Description ?? "",
                        ReleaseDate = t.ReleaseDate ?? "Unknown",
                        ChapterCount = t.Chapters.Count(),
                        LastUpdated = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            (DateTime?)null
                    })
                    .OrderBy(x => Guid.NewGuid()) // Better randomization
                    .Take(14)
                    .ToListAsync();

                _logger.LogInformation($"Returning {featuredTitles.Count} featured titles");
                return Ok(featuredTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching featured titles", error = ex.Message });
            }
        }

        /// <summary>
        /// Get popular titles for homepage (ordered by chapter count and ratings)
        /// GET: api/Titles/Popular
        /// </summary>
        [HttpGet("Popular")]
        public async Task<ActionResult<IEnumerable<TitleListDto>>> GetPopularTitles()
        {
            try
            {
                _logger.LogInformation("Fetching popular titles");

                var popularTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .Include(t => t.Chapters)
                    .ThenInclude(c => c.Views)
                    .Include(t => t.Ratings)
                    .Include(t => t.Bookmarks)
                    .Select(t => new
                    {
                        Title = t,
                        ChapterCount = t.Chapters.Count(),
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count(),
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => r.Value) : 0,
                        BookmarkCount = t.Bookmarks.Count(),
                        LastChapterDate = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            DateTime.MinValue,
                        PopularityScore = (t.Chapters.Count() * 2) +
                                       (t.Chapters.SelectMany(c => c.Views).Count() * 0.1) +
                                       (t.Ratings.Any() ? t.Ratings.Average(r => r.Value) * 10 : 0) +
                                       (t.Bookmarks.Count() * 5)
                    })
                    .OrderByDescending(x => x.PopularityScore)
                    .Take(20)
                    .ToListAsync();

                var result = popularTitles.Select(item => new TitleListDto
                {
                    Id = item.Title.Id,
                    OriginalTitle = item.Title.OriginalTitle ?? "Unknown Title",
                    EnglishTitle = item.Title.EnglishTitle ?? item.Title.OriginalTitle ?? "Unknown Title",
                    CoverImagePath = !string.IsNullOrEmpty(item.Title.CoverImagePath) ? item.Title.CoverImagePath : "/img/logo.png",
                    Type = item.Title.Type,
                    LatestChapter = item.ChapterCount > 0 ?
                        item.ChapterCount.ToString() :
                        "No chapters",
                    LastUpdated = item.LastChapterDate != DateTime.MinValue ? item.LastChapterDate : null,
                    ChapterCount = item.ChapterCount,
                    AverageRating = item.AverageRating,
                    BookmarkCount = item.BookmarkCount,
                    ReleaseDate = item.Title.ReleaseDate ?? "Unknown"
                }).ToList();

                _logger.LogInformation($"Returning {result.Count} popular titles");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching popular titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching popular titles", error = ex.Message });
            }
        }

        /// <summary>
        /// Get recent updates for homepage (ordered by actual update time)
        /// GET: api/Titles/RecentUpdates
        /// </summary>
        [HttpGet("RecentUpdates")]
        public async Task<ActionResult<IEnumerable<TitleUpdateDto>>> GetRecentUpdates()
        {
            try
            {
                _logger.LogInformation("Fetching recent updates");

                var recentUpdates = await _context.Titles
                    .Where(t => t.IsAvailable && t.Chapters.Any())
                    .Include(t => t.Teams)
                    .Include(t => t.Chapters)
                        .ThenInclude(c => c.Team)
                    .Select(t => new
                    {
                        Title = t,
                        LastChapterDate = t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate,
                        LatestChapterNumber = t.Chapters.Max(c => c.ChapterNumber),
                        LatestChapterName = t.Chapters.OrderByDescending(c => c.ReleaseDate).First().Name,
                        LatestChapterTeam = t.Chapters.OrderByDescending(c => c.ReleaseDate).First().Team,
                        LatestChapterVolume = t.Chapters.OrderByDescending(c => c.ReleaseDate).First().VolumeNumber
                    })
                    .OrderByDescending(x => x.LastChapterDate)
                    .Take(10)
                    .ToListAsync();

                var result = recentUpdates.Select(item => new TitleUpdateDto
                {
                    Id = item.Title.Id,
                    OriginalTitle = item.Title.OriginalTitle ?? "Unknown Title",
                    CoverImagePath = !string.IsNullOrEmpty(item.Title.CoverImagePath) ? item.Title.CoverImagePath : "/img/logo.png",
                    Description = !string.IsNullOrEmpty(item.Title.Description) && item.Title.Description.Length > 200
                        ? item.Title.Description.Substring(0, 200) + "..."
                        : item.Title.Description ?? "No description available",
                    TeamName = item.LatestChapterTeam?.Name ??
                              (item.Title.Teams.Any() ? item.Title.Teams.First().Name : "Unknown Team"),
                    TimeAgo = GetTimeAgo(item.LastChapterDate),
                    LatestChapter = $"Vol.{item.LatestChapterVolume} Ch.{item.LatestChapterNumber}" +
                                   (!string.IsNullOrEmpty(item.LatestChapterName) ? $": {item.LatestChapterName}" : ""),
                    LastUpdated = item.LastChapterDate
                }).ToList();

                _logger.LogInformation($"Returning {result.Count} recent updates");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent updates: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching recent updates", error = ex.Message });
            }
        }

        /// <summary>
        /// Get title details by encoded title name (for URL routing)
        /// GET: api/Titles/Details/{encodedTitle}
        /// </summary>
        [HttpGet("Details/{encodedTitle}")]
        public async Task<ActionResult<TitleDetailDto>> GetTitleByName(string encodedTitle)
        {
            try
            {
                var decodedTitle = Uri.UnescapeDataString(encodedTitle);
                _logger.LogInformation($"Looking for title: {decodedTitle}");

                var titleData = await _context.Titles
                    .Where(t => t.IsAvailable && t.OriginalTitle == decodedTitle)
                    .Include(t => t.Chapters)
                        .ThenInclude(c => c.Views)
                    .Include(t => t.Teams)
                    .Include(t => t.Authors)
                    .Include(t => t.Artists)
                    .Include(t => t.Categories)
                    .Include(t => t.Tags)
                    .Include(t => t.Ratings)
                    .Include(t => t.Bookmarks)
                        .ThenInclude(b => b.Folder)
                    .Select(t => new
                    {
                        Title = t,
                        ChapterCount = t.Chapters.Count(),
                        LatestChapterNumber = t.Chapters.Any() ? t.Chapters.Max(c => c.ChapterNumber) : 0,
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => (double)r.Value) : 0.0,
                        RatingCount = t.Ratings.Count(),
                        BookmarkCount = t.Bookmarks.Count(),
                        ViewCount = t.Chapters.SelectMany(c => c.Views).Count(),
                        LastUpdated = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            (DateTime?)null
                    })
                    .FirstOrDefaultAsync();

                if (titleData == null)
                {
                    _logger.LogWarning($"Title not found: {decodedTitle}");
                    return NotFound(new { message = "Title not found" });
                }

                var titleDto = new TitleDetailDto
                {
                    Id = titleData.Title.Id,
                    OriginalTitle = titleData.Title.OriginalTitle,
                    EnglishTitle = titleData.Title.EnglishTitle ?? titleData.Title.OriginalTitle,
                    Description = titleData.Title.Description ?? "",
                    CoverImagePath = !string.IsNullOrEmpty(titleData.Title.CoverImagePath) ? titleData.Title.CoverImagePath : "/img/logo.png",
                    BackgroundImagePath = titleData.Title.BackgroundImagePath,
                    Type = titleData.Title.Type,
                    StatusTitle = titleData.Title.StatusTitle ?? "Unknown",
                    StatusTranslation = titleData.Title.StatusTranslation ?? "Unknown",
                    ReleaseDate = titleData.Title.ReleaseDate ?? "Unknown",
                    AgeRestriction = titleData.Title.AgeRestriction,
                    ChapterCount = titleData.ChapterCount,
                    LatestChapter = titleData.LatestChapterNumber > 0 ? titleData.LatestChapterNumber.ToString() : "No chapters",
                    AverageRating = titleData.AverageRating,
                    RatingCount = titleData.RatingCount,
                    BookmarkCount = titleData.BookmarkCount,
                    ViewCount = titleData.ViewCount,
                    LastUpdated = titleData.LastUpdated,
                    Teams = titleData.Title.Teams.Select(team => team.Name).ToList(),
                    Authors = titleData.Title.Authors.Select(author => author.Name).ToList(),
                    Artists = titleData.Title.Artists.Select(artist => artist.Name).ToList(),
                    Categories = titleData.Title.Categories.Select(cat => cat.Name).ToList(),
                    Tags = titleData.Title.Tags.Select(tag => tag.Name).ToList()
                };

                return Ok(titleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching title details for {EncodedTitle}: {Error}", encodedTitle, ex.Message);
                return StatusCode(500, new { message = "Error fetching title details", error = ex.Message });
            }
        }

        // Helper methods
        private async Task<int> GetLatestChapterNumberForTitle(int titleId)
        {
            try
            {
                var latestChapter = await _context.Chapters
                    .Where(c => c.TitleId == titleId)
                    .OrderByDescending(c => c.ChapterNumber)
                    .FirstOrDefaultAsync();

                return latestChapter?.ChapterNumber ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting latest chapter for title {TitleId}", titleId);
                return 0;
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} min ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) > 1 ? "s" : "")} ago";

            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) > 1 ? "s" : "")} ago";
        }

        /// <summary>
        /// Get user's bookmarked titles with reading progress
        /// GET: api/Titles/UserBookmarks
        /// </summary>
        [HttpGet("UserBookmarks")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserBookmarks()
        {
            try
            {
                var userId = User?.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var userBookmarks = await _context.Bookmarks
                    .Where(b => b.UserId == userId)
                    .Include(b => b.Title)
                        .ThenInclude(t => t.Chapters)
                    .Include(b => b.Folder)
                    .Select(b => new
                    {
                        BookmarkId = b.Id,
                        TitleId = b.TitleId,
                        Title = b.Title.OriginalTitle,
                        EnglishTitle = b.Title.EnglishTitle,
                        CoverImage = b.Title.CoverImagePath,
                        Type = b.Title.Type,
                        LastReadChapter = b.LastReadChapter,
                        TotalChapters = b.Title.Chapters.Count(),
                        LatestChapter = b.Title.Chapters.Any() ? b.Title.Chapters.Max(c => c.ChapterNumber) : 0,
                        FolderName = b.Folder.Name,
                        AddedDate = b.AddedDate,
                        ReadingProgress = b.Title.Chapters.Any() ?
                            (double)b.LastReadChapter / b.Title.Chapters.Max(c => c.ChapterNumber) * 100 : 0,
                        LastUpdate = b.Title.Chapters.Any() ?
                            b.Title.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            (DateTime?)null
                    })
                    .OrderByDescending(b => b.AddedDate)
                    .ToListAsync();

                return Ok(userBookmarks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user bookmarks");
                return StatusCode(500, new { message = "Error fetching bookmarks", error = ex.Message });
            }
        }

        /// <summary>
        /// Get trending titles based on recent views and activity
        /// GET: api/Titles/Trending
        /// </summary>
        [HttpGet("Trending")]
        public async Task<ActionResult<IEnumerable<TitleListDto>>> GetTrendingTitles()
        {
            try
            {
                _logger.LogInformation("Fetching trending titles");

                var trendingTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .Include(t => t.Chapters)
                        .ThenInclude(c => c.Views)
                    .Include(t => t.Ratings)
                    .Include(t => t.Bookmarks)
                    .Select(t => new
                    {
                        Title = t,
                        ChapterCount = t.Chapters.Count(),
                        RecentViews = t.Chapters
                            .SelectMany(c => c.Views)
                            .Count(v => v.ViewDate >= DateTime.UtcNow.AddDays(-7)), // Views in last 7 days
                        RecentRatings = t.Ratings
                            .Count(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-30)), // Ratings in last 30 days
                        RecentBookmarks = t.Bookmarks
                            .Count(b => b.AddedDate >= DateTime.UtcNow.AddDays(-30)), // Bookmarks in last 30 days
                        AverageRating = t.Ratings.Any() ? t.Ratings.Average(r => (double)r.Value) : 0.0,
                        LastChapterDate = t.Chapters.Any() ?
                            t.Chapters.OrderByDescending(c => c.ReleaseDate).First().ReleaseDate :
                            DateTime.MinValue,
                        TrendingScore =
                            (t.Chapters.SelectMany(c => c.Views).Count(v => v.ViewDate >= DateTime.UtcNow.AddDays(-7)) * 1.0) +
                            (t.Ratings.Count(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-30)) * 5.0) +
                            (t.Bookmarks.Count(b => b.AddedDate >= DateTime.UtcNow.AddDays(-30)) * 10.0) +
                            (t.Chapters.Any() && t.Chapters.Max(c => c.ReleaseDate) >= DateTime.UtcNow.AddDays(-7) ? 20.0 : 0)
                    })
                    .Where(x => x.TrendingScore > 0)
                    .OrderByDescending(x => x.TrendingScore)
                    .Take(15)
                    .ToListAsync();

                var result = trendingTitles.Select(item => new TitleListDto
                {
                    Id = item.Title.Id,
                    OriginalTitle = item.Title.OriginalTitle ?? "Unknown Title",
                    EnglishTitle = item.Title.EnglishTitle ?? item.Title.OriginalTitle ?? "Unknown Title",
                    CoverImagePath = !string.IsNullOrEmpty(item.Title.CoverImagePath) ? item.Title.CoverImagePath : "/img/logo.png",
                    Type = item.Title.Type,
                    LatestChapter = item.ChapterCount > 0 ? item.ChapterCount.ToString() : "No chapters",
                    LastUpdated = item.LastChapterDate != DateTime.MinValue ? item.LastChapterDate : null,
                    ChapterCount = item.ChapterCount,
                    AverageRating = item.AverageRating,
                    BookmarkCount = item.RecentBookmarks, // Use recent bookmarks for trending
                    ReleaseDate = item.Title.ReleaseDate ?? "Unknown"
                }).ToList();

                _logger.LogInformation($"Returning {result.Count} trending titles");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trending titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching trending titles", error = ex.Message });
            }
        }

        /// <summary>
        /// Seed sample data for testing with chapters
        /// POST: api/Titles/SeedSampleData
        /// </summary>
        [HttpPost("SeedSampleData")]
        public async Task<ActionResult> SeedSampleData()
        {
            try
            {
                // Check if we already have titles
                var existingTitleCount = await _context.Titles.CountAsync();
                if (existingTitleCount > 5)
                {
                    return Ok(new { message = $"Database already has {existingTitleCount} titles. No seeding needed." });
                }

                // Get a user to assign as the updater (use first admin or create system user)
                var systemUser = await _context.Users.FirstOrDefaultAsync();
                if (systemUser == null)
                {
                    return BadRequest(new { error = "No users found in system. Create a user first." });
                }

                // Create sample titles with proper release dates
                var sampleTitles = new List<Title>
                {
                    new Title
                    {
                        OriginalTitle = "One Piece",
                        EnglishTitle = "One Piece",
                        Description = "The story of Monkey D. Luffy, a young man whose body gained the properties of rubber after unintentionally eating a Devil Fruit. Together with his crew of pirates, named the Straw Hat Pirates, Luffy explores the Grand Line in search of the world's ultimate treasure known as 'One Piece' in order to become the next Pirate King.",
                        Type = MangaType.Manga,
                        StatusTitle = "Ongoing",
                        StatusTranslation = "Ongoing",
                        ReleaseDate = "1997-07-22",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 13
                    },
                    new Title
                    {
                        OriginalTitle = "Solo Leveling",
                        EnglishTitle = "Solo Leveling",
                        Description = "In a world where hunters battle monsters that emerge from dungeons, Sung Jin-Woo is the weakest of all hunters, barely able to make a living. However, a mysterious System chooses him as its sole Player and in turn, gives him the unique ability to level up in strength and turn anyone he kills into a loyal minion called a Shadow.",
                        Type = MangaType.Manhwa,
                        StatusTitle = "Completed",
                        StatusTranslation = "Ongoing",
                        ReleaseDate = "2018-03-04",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 15
                    },
                    new Title
                    {
                        OriginalTitle = "Attack on Titan",
                        EnglishTitle = "Attack on Titan",
                        Description = "Humanity fights for survival against giant humanoid Titans behind massive walls. When the walls are breached, Eren Yeager vows to kill all Titans after witnessing his mother's death. A dark tale of war, politics, and the true nature of freedom.",
                        Type = MangaType.Manga,
                        StatusTitle = "Completed",
                        StatusTranslation = "Completed",
                        ReleaseDate = "2009-09-09",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 16
                    }
                };

                _context.Titles.AddRange(sampleTitles);
                await _context.SaveChangesAsync();

                // Add sample chapters for each title using your existing Chapter model
                foreach (var title in sampleTitles)
                {
                    var chapters = new List<Chapter>();
                    var chapterCount = _random.Next(5, 50); // Random number of chapters

                    for (int i = 1; i <= chapterCount; i++)
                    {
                        var chapterReleaseDate = DateTime.UtcNow.AddDays(-_random.Next(1, 365));

                        chapters.Add(new Chapter
                        {
                            TitleId = title.Id,
                            Name = $"Chapter {i}",
                            VolumeNumber = (i - 1) / 10 + 1, // Every 10 chapters = new volume
                            ChapterNumber = i,
                            CreatedDate = chapterReleaseDate.AddHours(-1), // Created slightly before release
                            ReleaseDate = chapterReleaseDate,
                            LastUpdatedAt = chapterReleaseDate,
                            UpdatedByUserId = systemUser.Id,
                            TeamId = null // Will be set when teams are available
                        });
                    }

                    _context.Chapters.AddRange(chapters);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully seeded {sampleTitles.Count} sample titles with chapters",
                    titles = sampleTitles.Select(t => new {
                        t.OriginalTitle,
                        t.EnglishTitle,
                        t.Type,
                        t.ReleaseDate,
                        ChapterCount = t.Chapters?.Count() ?? 0
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding sample data: {Error}", ex.Message);
                return StatusCode(500, new { error = "Error seeding sample data", details = ex.Message });
            }
        }
    }
}