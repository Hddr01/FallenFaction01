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

                var sampleTitles = await _context.Titles
                    .Take(3)
                    .Select(t => new { t.Id, t.OriginalTitle, t.EnglishTitle, t.IsAvailable })
                    .ToListAsync();

                return Ok(new
                {
                    TotalTitles = titleCount,
                    AvailableTitles = availableTitleCount,
                    TotalUsers = userCount,
                    TotalTeams = teamCount,
                    SampleTitles = sampleTitles,
                    DatabaseConnected = true
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

                var availableTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .ToListAsync();

                _logger.LogInformation($"Found {availableTitles.Count} available titles");

                if (!availableTitles.Any())
                {
                    _logger.LogWarning("No available titles found in database");
                    return Ok(new List<TitleFeaturedDto>());
                }

                // Randomize in memory to avoid database issues
                var randomizedTitles = availableTitles
                    .OrderBy(x => _random.Next())
                    .Take(14)
                    .ToList();

                var featuredTitles = new List<TitleFeaturedDto>();

                foreach (var title in randomizedTitles)
                {
                    var latestChapter = await GetLatestChapterNumberForTitle(title.Id);

                    featuredTitles.Add(new TitleFeaturedDto
                    {
                        Id = title.Id,
                        OriginalTitle = title.OriginalTitle ?? "Unknown Title",
                        EnglishTitle = title.EnglishTitle ?? title.OriginalTitle ?? "Unknown Title",
                        CoverImagePath = !string.IsNullOrEmpty(title.CoverImagePath) ? title.CoverImagePath : "/img/logo.png",
                        Type = title.Type,
                        LatestChapter = latestChapter > 0 ? latestChapter.ToString() : "1",
                        Description = title.Description ?? ""
                    });
                }

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
        /// Get popular titles for homepage
        /// GET: api/Titles/Popular
        /// </summary>
        [HttpGet("Popular")]
        public async Task<ActionResult<IEnumerable<TitleListDto>>> GetPopularTitles()
        {
            try
            {
                _logger.LogInformation("Fetching popular titles");

                var availableTitles = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .ToListAsync();

                if (!availableTitles.Any())
                {
                    _logger.LogWarning("No available titles found for popular titles");
                    return Ok(new List<TitleListDto>());
                }

                // Randomize in memory
                var randomizedTitles = availableTitles
                    .OrderBy(x => _random.Next())
                    .Take(20)
                    .ToList();

                var popularTitles = new List<TitleListDto>();

                foreach (var title in randomizedTitles)
                {
                    var latestChapter = await GetLatestChapterNumberForTitle(title.Id);

                    popularTitles.Add(new TitleListDto
                    {
                        Id = title.Id,
                        OriginalTitle = title.OriginalTitle ?? "Unknown Title",
                        EnglishTitle = title.EnglishTitle ?? title.OriginalTitle ?? "Unknown Title",
                        CoverImagePath = !string.IsNullOrEmpty(title.CoverImagePath) ? title.CoverImagePath : "/img/logo.png",
                        Type = title.Type,
                        LatestChapter = latestChapter > 0 ? latestChapter.ToString() : "1",
                        LastUpdated = DateTime.UtcNow.AddDays(-_random.Next(1, 30))
                    });
                }

                _logger.LogInformation($"Returning {popularTitles.Count} popular titles");
                return Ok(popularTitles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching popular titles: {Error}", ex.Message);
                return StatusCode(500, new { message = "Error fetching popular titles", error = ex.Message });
            }
        }

        /// <summary>
        /// Get recent updates for homepage
        /// GET: api/Titles/RecentUpdates
        /// </summary>
        [HttpGet("RecentUpdates")]
        public async Task<ActionResult<IEnumerable<TitleUpdateDto>>> GetRecentUpdates()
        {
            try
            {
                _logger.LogInformation("Fetching recent updates");

                var recentUpdates = await _context.Titles
                    .Where(t => t.IsAvailable)
                    .Include(t => t.Teams)
                    .OrderByDescending(t => t.Id) // Use ID for ordering instead of random
                    .Take(5)
                    .Select(t => new TitleUpdateDto
                    {
                        Id = t.Id,
                        OriginalTitle = t.OriginalTitle ?? "Unknown Title",
                        CoverImagePath = !string.IsNullOrEmpty(t.CoverImagePath) ? t.CoverImagePath : "/img/logo.png",
                        Description = !string.IsNullOrEmpty(t.Description) && t.Description.Length > 200
                            ? t.Description.Substring(0, 200) + "..."
                            : t.Description ?? "No description available",
                        TeamName = t.Teams.Any() ? t.Teams.First().Name : "Unknown Team",
                        TimeAgo = GetRandomTimeAgo()
                    })
                    .ToListAsync();

                _logger.LogInformation($"Returning {recentUpdates.Count} recent updates");
                return Ok(recentUpdates);
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
        public async Task<ActionResult<TitleFeaturedDto>> GetTitleByName(string encodedTitle)
        {
            try
            {
                var decodedTitle = Uri.UnescapeDataString(encodedTitle);
                _logger.LogInformation($"Looking for title: {decodedTitle}");

                var title = await _context.Titles
                    .Where(t => t.IsAvailable && t.OriginalTitle == decodedTitle)
                    .FirstOrDefaultAsync();

                if (title == null)
                {
                    _logger.LogWarning($"Title not found: {decodedTitle}");
                    return NotFound(new { message = "Title not found" });
                }

                var latestChapter = await GetLatestChapterNumberForTitle(title.Id);

                var titleDto = new TitleFeaturedDto
                {
                    Id = title.Id,
                    OriginalTitle = title.OriginalTitle,
                    EnglishTitle = title.EnglishTitle ?? title.OriginalTitle,
                    CoverImagePath = !string.IsNullOrEmpty(title.CoverImagePath) ? title.CoverImagePath : "/img/logo.png",
                    Type = title.Type,
                    LatestChapter = latestChapter > 0 ? latestChapter.ToString() : "1",
                    Description = title.Description ?? ""
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

                return latestChapter?.ChapterNumber ?? _random.Next(1, 100); // Mock data if no chapters
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting latest chapter for title {TitleId}", titleId);
                return _random.Next(1, 100); // Return mock data on error
            }
        }

        private string GetRandomTimeAgo()
        {
            var timeOptions = new[]
            {
                "5 min ago", "15 min ago", "30 min ago", "1 hour ago",
                "2 hours ago", "5 hours ago", "1 day ago", "2 days ago"
            };
            return timeOptions[_random.Next(timeOptions.Length)];
        }

        /// <summary>
        /// Seed sample data for testing
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

                // Create sample titles
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
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 15
                    },
                    new Title
                    {
                        OriginalTitle = "Tower of God",
                        EnglishTitle = "Tower of God",
                        Description = "Follow Bam as he climbs the mysterious Tower to find his friend Rachel. The Tower is said to grant any wish to those who reach the top. Each floor presents new challenges and powerful adversaries in this thrilling webtoon adventure.",
                        Type = MangaType.Webtoon,
                        StatusTitle = "Ongoing",
                        StatusTranslation = "Ongoing",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 13
                    },
                    new Title
                    {
                        OriginalTitle = "Attack on Titan",
                        EnglishTitle = "Attack on Titan",
                        Description = "Humanity fights for survival against giant humanoid Titans behind massive walls. When the walls are breached, Eren Yeager vows to kill all Titans after witnessing his mother's death. A dark tale of war, politics, and the true nature of freedom.",
                        Type = MangaType.Manga,
                        StatusTitle = "Completed",
                        StatusTranslation = "Completed",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 16
                    },
                    new Title
                    {
                        OriginalTitle = "Demon Slayer",
                        EnglishTitle = "Demon Slayer",
                        Description = "Tanjiro Kamado becomes a demon slayer to save his sister Nezuko and avenge his family after they are slaughtered by demons. Follow his journey through intense training and battles against powerful demons in Taisho-era Japan.",
                        Type = MangaType.Manga,
                        StatusTitle = "Completed",
                        StatusTranslation = "Completed",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 13
                    },
                    new Title
                    {
                        OriginalTitle = "Naruto",
                        EnglishTitle = "Naruto",
                        Description = "The story of Naruto Uzumaki, a young ninja who dreams of becoming the Hokage, the leader of his village. Despite being shunned by the villagers for harboring a dangerous beast within him, Naruto works hard to gain their respect.",
                        Type = MangaType.Manga,
                        StatusTitle = "Completed",
                        StatusTranslation = "Completed",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 13
                    },
                    new Title
                    {
                        OriginalTitle = "My Hero Academia",
                        EnglishTitle = "My Hero Academia",
                        Description = "In a world where people with superpowers called 'Quirks' are the norm, Izuku Midoriya is born without any powers. Despite this, he dreams of becoming a hero like his idol All Might.",
                        Type = MangaType.Manga,
                        StatusTitle = "Ongoing",
                        StatusTranslation = "Ongoing",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 13
                    },
                    new Title
                    {
                        OriginalTitle = "Dragon Ball",
                        EnglishTitle = "Dragon Ball",
                        Description = "Follow Goku's adventures as he searches for the Dragon Balls, seven mystical orbs that grant any wish when gathered together. From his childhood through his battles against increasingly powerful foes.",
                        Type = MangaType.Manga,
                        StatusTitle = "Completed",
                        StatusTranslation = "Completed",
                        CoverImagePath = "/img/logo.png",
                        BackgroundImagePath = "/img/logo.png",
                        IsAvailable = true,
                        AreCommentsEnabled = true,
                        AreChapterCommentsEnabled = true,
                        AgeRestriction = 13
                    }
                };

                _context.Titles.AddRange(sampleTitles);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully seeded {sampleTitles.Count} sample titles",
                    titles = sampleTitles.Select(t => new { t.OriginalTitle, t.EnglishTitle, t.Type })
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