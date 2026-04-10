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
                return StatusCode(500, new { message = "Error fetching bookmark folders" });
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
                return StatusCode(500, new { message = "Error adding bookmark" });
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
                return StatusCode(500, new { message = "Error removing bookmark" });
            }
        }

        [HttpGet("GetBookmarkStats")]
        public async Task<ActionResult<BookmarkStatsDto>> GetBookmarkStats(int titleId)
        {
            try
            {
                var title = await _context.Titles.FirstOrDefaultAsync(t => t.Id == titleId);
                if (title == null)
                    return NotFound("Title not found");

                var bookmarkCount = await _context.Bookmarks
                    .Where(b => b.TitleId == titleId)
                    .CountAsync();

                // Group by folder NAME only — every user has their own folder IDs,
                // so grouping by FolderId would give one row per user instead of one per status.
                var rawDistribution = await _context.Bookmarks
                    .Where(b => b.TitleId == titleId)
                    .Include(b => b.Folder)
                    .GroupBy(b => b.Folder.Name)
                    .Select(g => new
                    {
                        FolderName = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // The 5 standard folder names
                var standardNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Reading", "Completed", "On Hold", "Plan to Read", "Dropped"
                };

                var distribution = new List<BookmarkFolderDistributionDto>();

                int othersCount = 0;

                foreach (var row in rawDistribution)
                {
                    if (standardNames.Contains(row.FolderName))
                    {
                        distribution.Add(new BookmarkFolderDistributionDto
                        {
                            FolderId = 0,  // not meaningful for cross-user stats
                            FolderName = row.FolderName,
                            Count = row.Count,
                            Percentage = bookmarkCount > 0
                                ? Math.Round((double)row.Count / bookmarkCount * 100, 1) : 0
                        });
                    }
                    else
                    {
                        // Custom folder → accumulate into "Others"
                        othersCount += row.Count;
                    }
                }

                // Add the "Others" row only if there are custom-folder bookmarks
                if (othersCount > 0)
                {
                    distribution.Add(new BookmarkFolderDistributionDto
                    {
                        FolderId = -1,  // sentinel — no real folder ID
                        FolderName = "Others",
                        Count = othersCount,
                        Percentage = bookmarkCount > 0
                            ? Math.Round((double)othersCount / bookmarkCount * 100, 1) : 0
                    });
                }

                distribution = distribution.OrderByDescending(d => d.Count).ToList();

                return new BookmarkStatsDto
                {
                    TitleId = titleId,
                    TotalBookmarks = bookmarkCount,
                    FolderDistribution = distribution
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookmark stats for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error fetching bookmark stats" });
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
                return StatusCode(500, new { message = "Error creating folder" });
            }
        }

        private async Task EnsureDefaultFoldersExist(string userId)
        {
            try
            {
                var existingFolders = await _context.BookmarkFolders
                    .Where(f => f.UserId == userId)
                    .ToListAsync();

                // The 5 canonical standard folders (name is the key, order is fixed)
                var standards = new[]
                {
                    new { Name = "Reading",      IsDefault = true,  Order = 1 },
                    new { Name = "Completed",    IsDefault = false, Order = 2 },
                    new { Name = "On Hold",      IsDefault = false, Order = 3 },
                    new { Name = "Plan to Read", IsDefault = false, Order = 4 },
                    new { Name = "Dropped",      IsDefault = false, Order = 5 },
                };

                bool changed = false;
                foreach (var std in standards)
                {
                    if (!existingFolders.Any(f => f.Name == std.Name))
                    {
                        _context.BookmarkFolders.Add(new BookmarkFolder
                        {
                            Name = std.Name,
                            UserId = userId,
                            IsDefault = std.IsDefault,
                            DisplayOrder = std.Order,
                            CreatedAt = DateTime.UtcNow
                        });
                        changed = true;
                    }
                }

                if (changed)
                    await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring default folders for user {UserId}", userId);
                throw;
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
                        OriginalTitle = b.Title.OriginalTitle,
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
                return StatusCode(500, new { message = "Error fetching bookmarks" });
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
                return StatusCode(500, new { message = "Error updating folder" });
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
                return StatusCode(500, new { message = "Error deleting folder" });
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
                return StatusCode(500, new { message = "Error updating reading progress" });
            }
        }

        /// <summary>
        /// Update bookmark status AND move to the matching folder.
        /// PUT: api/Bookmarks/UpdateStatus
        /// </summary>
        [HttpPut("UpdateStatus")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateBookmarkStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            AppUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var titleExists = await _context.Titles.AnyAsync(t => t.Id == request.TitleId);
                if (!titleExists)
                    return NotFound("Title not found");

                // Ensure all default folders exist for this user
                await EnsureDefaultFoldersExist(user.Id);

                // Map the status string to the matching folder name
                // Folder names are the canonical source of truth (set in EnsureDefaultFoldersExist)
                var statusToFolderName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "reading",       "Reading"      },
                    { "completed",     "Completed"    },
                    { "on-hold",       "On Hold"      },
                    { "plan-to-read",  "Plan to Read" },
                    { "dropped",       "Dropped"      },
                    { "favorites",     "Favorites"    }
                };

                if (!statusToFolderName.TryGetValue(request.Status, out var targetFolderName))
                    return BadRequest(new
                    {
                        message = $"Unknown status '{request.Status}'. " +
                        "Valid values: reading, completed, on-hold, plan-to-read, dropped, favorites"
                    });

                // Find the target folder for this user (must already exist after EnsureDefaultFoldersExist)
                var targetFolder = await _context.BookmarkFolders
                    .FirstOrDefaultAsync(f => f.UserId == user.Id && f.Name == targetFolderName);

                if (targetFolder == null)
                {
                    // Safety-net: create the folder on the fly (shouldn't normally be needed)
                    var maxOrder = await _context.BookmarkFolders
                        .Where(f => f.UserId == user.Id)
                        .Select(f => (int?)f.DisplayOrder)
                        .MaxAsync() ?? 0;

                    targetFolder = new BookmarkFolder
                    {
                        Name = targetFolderName,
                        UserId = user.Id,
                        IsDefault = false,
                        DisplayOrder = maxOrder + 1,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.BookmarkFolders.Add(targetFolder);
                    await _context.SaveChangesAsync();
                }

                // Find or create the bookmark
                var bookmark = await _context.Bookmarks
                    .FirstOrDefaultAsync(b => b.TitleId == request.TitleId && b.UserId == user.Id);

                if (bookmark == null)
                {
                    bookmark = new Bookmark
                    {
                        TitleId = request.TitleId,
                        FolderId = targetFolder.Id,  // ← placed directly into the right folder
                        UserId = user.Id,
                        AddedDate = DateTime.UtcNow,
                        LastReadChapter = 0,
                        Status = request.Status
                    };
                    _context.Bookmarks.Add(bookmark);
                }
                else
                {
                    // Move to the matching folder AND update the status field
                    bookmark.FolderId = targetFolder.Id;  // ← THE KEY FIX
                    bookmark.Status = request.Status;
                    _context.Bookmarks.Update(bookmark);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Bookmark moved to '{targetFolderName}'",
                    data = new
                    {
                        bookmarkId = bookmark.Id,
                        status = bookmark.Status,
                        folderId = bookmark.FolderId,
                        folderName = targetFolderName,
                        titleId = bookmark.TitleId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bookmark status for user {UserId}", user?.Id);
                return StatusCode(500, new { message = "Error updating bookmark status" });
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
                return StatusCode(500, new { message = "Error removing bookmark" });
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
                return StatusCode(500, new { message = "Error checking bookmark" });
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
                return StatusCode(500, new { message = "Error getting reading progress" });
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
                return StatusCode(500, new { message = "Error getting user bookmark" });
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