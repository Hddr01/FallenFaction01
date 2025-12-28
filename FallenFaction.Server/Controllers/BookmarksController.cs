// Controllers/BookmarksController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using Microsoft.AspNetCore.Identity;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Bookmarks;

namespace FallenFaction.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookmarksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<BookmarksController> _logger;

        public BookmarksController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<BookmarksController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("GetFolders")]
        [AllowAnonymous]
        public async Task<ActionResult<BookmarkFoldersResponseDto>> GetFolders([FromQuery] int? titleId = null)
        {
            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return new BookmarkFoldersResponseDto
                    {
                        Folders = new List<BookmarkFolderDto>(),
                        CurrentBookmark = null
                    };
                }

                await EnsureDefaultFoldersExist(user.Id);

                var folders = await _context.BookmarkFolders
                    .Where(f => f.UserId == user.Id)
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new BookmarkFolderDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        IsDefault = f.IsDefault,
                        DisplayOrder = f.DisplayOrder,
                        Count = f.Bookmarks.Count
                    })
                    .ToListAsync();

                BookmarkDto? currentBookmark = null;
                if (titleId.HasValue)
                {
                    var bookmark = await _context.Bookmarks
                        .Include(b => b.Folder)
                        .FirstOrDefaultAsync(b => b.TitleId == titleId.Value && b.UserId == user.Id);

                    if (bookmark != null)
                    {
                        currentBookmark = new BookmarkDto
                        {
                            Id = bookmark.Id,
                            TitleId = bookmark.TitleId,
                            FolderId = bookmark.FolderId,
                            FolderName = bookmark.Folder.Name,
                            AddedDate = bookmark.AddedDate,
                            LastReadChapter = bookmark.LastReadChapter
                        };
                    }
                }

                return new BookmarkFoldersResponseDto
                {
                    Folders = folders,
                    CurrentBookmark = currentBookmark
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookmark folders for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error fetching bookmark folders", error = ex.Message });
            }
        }

        [HttpPost("AddBookmark")]
        [Authorize]
        public async Task<IActionResult> AddBookmark([FromBody] AddBookmarkRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Check if title exists
                var titleExists = await _context.Titles.AnyAsync(t => t.Id == request.TitleId);
                if (!titleExists)
                {
                    return NotFound("Title not found");
                }

                // Check if folder exists and belongs to user
                var folder = await _context.BookmarkFolders
                    .FirstOrDefaultAsync(f => f.Id == request.FolderId && f.UserId == user.Id);
                if (folder == null)
                {
                    return NotFound("Folder not found");
                }

                // Check if the title is already bookmarked by this user
                var existingBookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.TitleId == request.TitleId && b.UserId == user.Id);

                if (existingBookmark != null)
                {
                    // Move to new folder
                    existingBookmark.FolderId = request.FolderId;
                    _context.Bookmarks.Update(existingBookmark);
                    await _context.SaveChangesAsync();
                    return Ok(new { id = existingBookmark.Id, message = "Bookmark moved to new folder" });
                }

                // Create a new bookmark
                var bookmark = new Bookmark
                {
                    TitleId = request.TitleId,
                    FolderId = request.FolderId,
                    UserId = user.Id,
                    AddedDate = DateTime.UtcNow,
                    LastReadChapter = 0
                };

                _context.Bookmarks.Add(bookmark);
                await _context.SaveChangesAsync();

                return Ok(new { id = bookmark.Id, message = "Bookmark added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bookmark for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error adding bookmark", error = ex.Message });
            }
        }

        [HttpPost("RemoveBookmark")]
        [Authorize]
        public async Task<IActionResult> RemoveBookmark([FromBody] RemoveBookmarkRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.Id == request.BookmarkId && b.UserId == user.Id);

                if (bookmark == null)
                {
                    return NotFound("Bookmark not found");
                }

                _context.Bookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Bookmark removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing bookmark for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error removing bookmark", error = ex.Message });
            }
        }

        [HttpGet("GetBookmarkStats")]
        public async Task<ActionResult<BookmarkStatsDto>> GetBookmarkStats(int titleId)
        {
            try
            {
                // Check if title exists
                var title = await _context.Titles
                    .FirstOrDefaultAsync(t => t.Id == titleId);

                if (title == null)
                {
                    return NotFound("Title not found");
                }

                // Count bookmarks for this title
                var bookmarkCount = await _context.Bookmarks
                    .Where(b => b.TitleId == titleId)
                    .CountAsync();

                // Get distribution by folder (top 5 folders)
                var folderDistribution = await _context.Bookmarks
                    .Where(b => b.TitleId == titleId)
                    .Include(b => b.Folder)
                    .GroupBy(b => new { b.FolderId, b.Folder.Name })
                    .Select(g => new BookmarkFolderDistributionDto
                    {
                        FolderId = g.Key.FolderId,
                        FolderName = g.Key.Name,
                        Count = g.Count(),
                        Percentage = 0 // Will calculate below
                    })
                    .OrderByDescending(f => f.Count)
                    .Take(5)
                    .ToListAsync();

                // Calculate percentages
                if (bookmarkCount > 0)
                {
                    foreach (var folder in folderDistribution)
                    {
                        folder.Percentage = Math.Round((double)folder.Count / bookmarkCount * 100, 1);
                    }
                }

                return new BookmarkStatsDto
                {
                    TitleId = titleId,
                    TotalBookmarks = bookmarkCount,
                    FolderDistribution = folderDistribution
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookmark stats for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error fetching bookmark stats", error = ex.Message });
            }
        }

        [HttpPost("CreateFolder")]
        [Authorize]
        public async Task<ActionResult<BookmarkFolderDto>> CreateFolder([FromBody] CreateFolderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Check if folder name already exists for this user
                var existingFolder = await _context.BookmarkFolders
                    .FirstOrDefaultAsync(f => f.UserId == user.Id && f.Name == request.Name);

                if (existingFolder != null)
                {
                    return BadRequest("A folder with this name already exists");
                }

                // Get max display order
                var maxOrder = await _context.BookmarkFolders
                    .Where(f => f.UserId == user.Id)
                    .Select(f => (int?)f.DisplayOrder)
                    .MaxAsync() ?? 0;

                var folder = new BookmarkFolder
                {
                    Name = request.Name,
                    UserId = user.Id,
                    IsDefault = false,
                    DisplayOrder = maxOrder + 1,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BookmarkFolders.Add(folder);
                await _context.SaveChangesAsync();

                return new BookmarkFolderDto
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    IsDefault = folder.IsDefault,
                    DisplayOrder = folder.DisplayOrder,
                    Count = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error creating folder", error = ex.Message });
            }
        }

        private async Task EnsureDefaultFoldersExist(string userId)
        {
            try
            {
                var existingFolders = await _context.BookmarkFolders
                    .Where(f => f.UserId == userId)
                    .ToListAsync();

                if (!existingFolders.Any())
                {
                    var defaultFolders = new[]
                    {
                        new BookmarkFolder { Name = "Reading", UserId = userId, IsDefault = true, DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
                        new BookmarkFolder { Name = "Plan to Read", UserId = userId, IsDefault = false, DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
                        new BookmarkFolder { Name = "Completed", UserId = userId, IsDefault = false, DisplayOrder = 3, CreatedAt = DateTime.UtcNow },
                        new BookmarkFolder { Name = "Dropped", UserId = userId, IsDefault = false, DisplayOrder = 4, CreatedAt = DateTime.UtcNow },
                        new BookmarkFolder { Name = "Favorites", UserId = userId, IsDefault = false, DisplayOrder = 5, CreatedAt = DateTime.UtcNow }
                    };

                    _context.BookmarkFolders.AddRange(defaultFolders);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring default folders exist for user {UserId}", userId);
                throw; // Re-throw to let the caller handle it
            }
        }

        /// <summary>
        /// Get user's bookmarks by folder
        /// GET: api/Bookmarks/GetBookmarksByFolder/{folderId}
        /// </summary>
        [HttpGet("GetBookmarksByFolder/{folderId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<BookmarkDto>>> GetBookmarksByFolder(int folderId)
        {
            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Verify folder belongs to user
                var folder = await _context.BookmarkFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == user.Id);

                if (folder == null)
                {
                    return NotFound("Folder not found");
                }

                var bookmarks = await _context.Bookmarks
                    .Where(b => b.FolderId == folderId && b.UserId == user.Id)
                    .Include(b => b.Title)
                    .Include(b => b.Folder)
                    .Select(b => new BookmarkDto
                    {
                        Id = b.Id,
                        TitleId = b.TitleId,
                        FolderId = b.FolderId,
                        FolderName = b.Folder.Name,
                        TitleName = b.Title.EnglishTitle ?? b.Title.OriginalTitle,
                        CoverImage = b.Title.CoverImagePath ?? "/img/logo.png",
                        AddedDate = b.AddedDate,
                        LastReadChapter = b.LastReadChapter
                    })
                    .OrderByDescending(b => b.AddedDate)
                    .ToListAsync();

                return Ok(bookmarks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookmarks by folder for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error fetching bookmarks", error = ex.Message });
            }
        }

        /// <summary>
        /// Update folder details
        /// PUT: api/Bookmarks/UpdateFolder/{folderId}
        /// </summary>
        [HttpPut("UpdateFolder/{folderId}")]
        [Authorize]
        public async Task<IActionResult> UpdateFolder(int folderId, [FromBody] UpdateFolderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var folder = await _context.BookmarkFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == user.Id);

                if (folder == null)
                {
                    return NotFound("Folder not found");
                }

                // Check if another folder with the same name already exists
                var existingFolder = await _context.BookmarkFolders
                    .FirstOrDefaultAsync(f => f.UserId == user.Id && f.Name == request.Name && f.Id != folderId);

                if (existingFolder != null)
                {
                    return BadRequest("A folder with this name already exists");
                }

                folder.Name = request.Name;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Folder updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error updating folder", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete folder (moves bookmarks to default folder)
        /// DELETE: api/Bookmarks/DeleteFolder/{folderId}
        /// </summary>
        [HttpDelete("DeleteFolder/{folderId}")]
        [Authorize]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var folder = await _context.BookmarkFolders
                    .Include(f => f.Bookmarks)
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == user.Id);

                if (folder == null)
                {
                    return NotFound("Folder not found");
                }

                if (folder.IsDefault)
                {
                    return BadRequest("Cannot delete default folder");
                }

                // Move bookmarks to default folder if they exist
                if (folder.Bookmarks.Any())
                {
                    var defaultFolder = await _context.BookmarkFolders
                        .FirstOrDefaultAsync(f => f.UserId == user.Id && f.IsDefault);

                    if (defaultFolder == null)
                    {
                        await EnsureDefaultFoldersExist(user.Id);
                        defaultFolder = await _context.BookmarkFolders
                            .FirstOrDefaultAsync(f => f.UserId == user.Id && f.IsDefault);
                    }

                    if (defaultFolder != null)
                    {
                        foreach (var bookmark in folder.Bookmarks)
                        {
                            bookmark.FolderId = defaultFolder.Id;
                        }
                    }
                }

                _context.BookmarkFolders.Remove(folder);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Folder deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error deleting folder", error = ex.Message });
            }
        }

        /// <summary>
        /// Update last read chapter
        /// POST: api/Bookmarks/UpdateLastRead
        /// </summary>
        [HttpPost("UpdateLastRead")]
        [Authorize]
        public async Task<IActionResult> UpdateLastRead([FromBody] UpdateLastReadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.TitleId == request.TitleId && b.UserId == user.Id);

                if (bookmark == null)
                {
                    return NotFound("Bookmark not found");
                }

                bookmark.LastReadChapter = request.ChapterNumber;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Reading progress updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reading progress for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error updating reading progress", error = ex.Message });
            }
        }

        /// <summary>
        /// Update bookmark status (reading, completed, on-hold, plan-to-read, dropped)
        /// PUT: api/Bookmarks/UpdateStatus
        /// </summary>
        [HttpPut("UpdateStatus")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateBookmarkStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Check if title exists
                var titleExists = await _context.Titles.AnyAsync(t => t.Id == request.TitleId);
                if (!titleExists)
                {
                    return NotFound("Title not found");
                }

                // Find existing bookmark
                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.TitleId == request.TitleId && b.UserId == user.Id);

                if (bookmark == null)
                {
                    // If bookmark doesn't exist, create it with the specified status
                    // First, ensure default folders exist and get one
                    await EnsureDefaultFoldersExist(user.Id);
                    var defaultFolder = await _context.BookmarkFolders
                        .FirstOrDefaultAsync(f => f.UserId == user.Id && f.IsDefault);

                    if (defaultFolder == null)
                    {
                        return StatusCode(500, new { message = "Failed to create default folder" });
                    }

                    bookmark = new Bookmark
                    {
                        TitleId = request.TitleId,
                        FolderId = defaultFolder.Id,
                        UserId = user.Id,
                        AddedDate = DateTime.UtcNow,
                        LastReadChapter = 0,
                        Status = request.Status
                    };

                    _context.Bookmarks.Add(bookmark);
                }
                else
                {
                    // Update existing bookmark status
                    bookmark.Status = request.Status;
                    _context.Bookmarks.Update(bookmark);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Bookmark status updated to {request.Status}",
                    data = new
                    {
                        bookmarkId = bookmark.Id,
                        status = bookmark.Status,
                        titleId = bookmark.TitleId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bookmark status for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error updating bookmark status", error = ex.Message });
            }
        }

        /// <summary>
        /// Remove bookmark by titleId (DELETE method)
        /// DELETE: api/Bookmarks/RemoveBookmark?titleId={titleId}
        /// </summary>
        [HttpDelete("RemoveBookmark")]
        [Authorize]
        public async Task<IActionResult> RemoveBookmarkByTitleId([FromQuery] int titleId)
        {
            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.TitleId == titleId && b.UserId == user.Id);

                if (bookmark == null)
                {
                    return NotFound(new { success = false, message = "Bookmark not found" });
                }

                // CRITICAL: Save reading progress before deleting bookmark
                if (bookmark.LastReadChapter > 0)
                {
                    var existingProgress = await _context.ReadingProgress
                        .FirstOrDefaultAsync(rp => rp.TitleId == titleId && rp.UserId == user.Id);

                    if (existingProgress != null)
                    {
                        // Update existing progress record
                        existingProgress.LastReadChapter = bookmark.LastReadChapter;
                        existingProgress.LastReadDate = DateTime.UtcNow;
                        _context.ReadingProgress.Update(existingProgress);
                    }
                    else
                    {
                        // Create new progress record
                        var progress = new ReadingProgress
                        {
                            TitleId = titleId,
                            UserId = user.Id,
                            LastReadChapter = bookmark.LastReadChapter,
                            LastReadDate = DateTime.UtcNow
                        };
                        _context.ReadingProgress.Add(progress);
                    }
                }

                // Now remove the bookmark
                _context.Bookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Bookmark removed successfully, reading progress preserved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing bookmark for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error removing bookmark", error = ex.Message });
            }
        }

        /// <summary>
        /// Check if user has bookmarked a title
        /// GET: api/Bookmarks/CheckBookmark?titleId={titleId}
        /// </summary>
        [HttpGet("CheckBookmark")]
        [Authorize]
        public async Task<ActionResult<object>> CheckBookmark([FromQuery] int titleId)
        {
            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.TitleId == titleId && b.UserId == user.Id);

                return Ok(new
                {
                    isBookmarked = bookmark != null,
                    bookmarkId = bookmark?.Id,
                    status = bookmark?.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking bookmark for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error checking bookmark", error = ex.Message });
            }
        }

        /// <summary>
        /// NEW METHOD: Get reading progress for a title (independent of bookmark)
        /// GET: api/Bookmarks/GetReadingProgress?titleId={titleId}
        /// </summary>
        [HttpGet("GetReadingProgress")]
        [Authorize]
        public async Task<ActionResult<object>> GetReadingProgress([FromQuery] int titleId)
        {
            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // First check if there's a bookmark with reading progress
                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.TitleId == titleId && b.UserId == user.Id);

                if (bookmark != null && bookmark.LastReadChapter > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        hasProgress = true,
                        lastReadChapter = bookmark.LastReadChapter,
                        titleId = titleId
                    });
                }

                // If no bookmark or no progress, check ReadingProgress table
                var progress = await _context.ReadingProgress
                    .FirstOrDefaultAsync(rp => rp.TitleId == titleId && rp.UserId == user.Id);

                if (progress != null)
                {
                    return Ok(new
                    {
                        success = true,
                        hasProgress = true,
                        lastReadChapter = progress.LastReadChapter,
                        titleId = titleId
                    });
                }

                return Ok(new
                {
                    success = true,
                    hasProgress = false,
                    lastReadChapter = 0,
                    titleId = titleId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reading progress for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error getting reading progress", error = ex.Message });
            }
        }


        /// <summary>
        /// MODIFIED: Get user's bookmark for a specific title
        /// Now includes reading progress even if bookmark doesn't exist
        /// GET: api/Bookmarks/GetUserBookmark?titleId={titleId}
        /// </summary>
        [HttpGet("GetUserBookmark")]
        [Authorize]
        public async Task<ActionResult<object>> GetUserBookmark([FromQuery] int titleId)
        {
            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var bookmark = await _context.Bookmarks
                    .Include(b => b.Folder)
                    .Include(b => b.Title)
                    .FirstOrDefaultAsync(b => b.TitleId == titleId && b.UserId == user.Id);

                // Check for reading progress even if no bookmark exists
                var progress = await _context.ReadingProgress
                    .FirstOrDefaultAsync(rp => rp.TitleId == titleId && rp.UserId == user.Id);

                int lastReadChapter = 0;

                // Priority: bookmark progress > separate progress record
                if (bookmark != null && bookmark.LastReadChapter > 0)
                {
                    lastReadChapter = bookmark.LastReadChapter;
                }
                else if (progress != null)
                {
                    lastReadChapter = progress.LastReadChapter;
                }

                // If bookmark exists, return full data
                if (bookmark != null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            bookmarkId = bookmark.Id,
                            titleId = bookmark.TitleId,
                            folderId = bookmark.FolderId,
                            folderName = bookmark.Folder.Name,
                            status = bookmark.Status,
                            lastReadChapter = lastReadChapter, // Use combined progress
                            addedDate = bookmark.AddedDate,
                            hasBookmark = true
                        }
                    });
                }

                // If no bookmark but has reading progress, return progress only
                if (lastReadChapter > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            bookmarkId = (int?)null,
                            titleId = titleId,
                            folderId = (int?)null,
                            folderName = (string?)null,
                            status = (string?)null,
                            lastReadChapter = lastReadChapter,
                            addedDate = (DateTime?)null,
                            hasBookmark = false
                        }
                    });
                }

                return NotFound(new { success = false, message = "No bookmark or reading progress found for this title" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user bookmark for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error getting user bookmark", error = ex.Message });
            }
        }
        /// <summary>
        /// Health check endpoint
        /// GET: api/Bookmarks/health
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "BookmarksController"
            });
        }
    }
}