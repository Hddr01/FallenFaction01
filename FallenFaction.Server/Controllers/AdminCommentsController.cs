// Controllers/AdminCommentsController.cs - Complete Fixed Version
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Moderator")]
    public class AdminCommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AdminCommentsController> _logger;
        private readonly ICommentService _commentService;

        public AdminCommentsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<AdminCommentsController> logger,
            ICommentService commentService
            )
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _commentService = commentService;
        }

        /// <summary>
        /// Test endpoint to verify controller is working
        /// GET: api/AdminComments/test
        /// </summary>
        [HttpGet("test")]
        [AllowAnonymous] // Remove auth for testing
        public ActionResult<object> Test()
        {
            _logger.LogInformation("AdminComments test endpoint called");
            return Ok(new
            {
                message = "AdminCommentsController is working!",
                timestamp = DateTime.UtcNow,
                user = User?.Identity?.Name ?? "Anonymous"
            });
        }


        [HttpGet("GetCommentStats")]
        public async Task<ActionResult<CommentStatsDto>> GetCommentStats([FromQuery] int targetId, [FromQuery] int targetType)
        {
            try
            {
                var stats = await _commentService.GetCommentStatsAsync(targetId, targetType);
                return Ok(stats);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        /// <summary>
        /// Get comment statistics for admin dashboard (includes deleted comments)
        /// GET: api/AdminComments/GetStats
        /// </summary>
        [HttpGet("GetStats")]
        public async Task<ActionResult<AdminCommentStatsDto>> GetCommentStats()
        {
            try
            {
                _logger.LogInformation("Getting comment statistics for admin");

                var today = DateTime.UtcNow.Date;

                // Get comprehensive comment statistics
                var totalComments = await _context.Comments.CountAsync(c => !c.IsDeleted);
                var deletedComments = await _context.Comments.CountAsync(c => c.IsDeleted); // ✅ NEW
                var todayComments = await _context.Comments
                    .CountAsync(c => c.PostedDate >= today && !c.IsDeleted);
                var todayDeleted = await _context.Comments
                    .CountAsync(c => c.DeletedAt >= today); // ✅ NEW

                // Placeholder for reported comments
                var reportedComments = await _context.Comments
                    .CountAsync(c => c.DislikesCount > c.LikesCount && c.DislikesCount > 5 && !c.IsDeleted);

                // Active commenters in last 30 days (excluding deleted comments)
                var activeCommenters = await _context.Comments
                    .Where(c => c.PostedDate >= DateTime.UtcNow.AddDays(-30) && !c.IsDeleted)
                    .Select(c => c.UserId)
                    .Distinct()
                    .CountAsync();

                var stats = new AdminCommentStatsDto
                {
                    TotalComments = totalComments,
                    DeletedComments = deletedComments, // ✅ NEW
                    TodayComments = todayComments,
                    TodayDeleted = todayDeleted, // ✅ NEW
                    ReportedComments = reportedComments,
                    ActiveCommenters = activeCommenters
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment statistics");
                return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all comments for admin management with filtering and pagination (includes deleted)
        /// GET: api/AdminComments/GetAllComments
        /// </summary>
        [HttpGet("GetAllComments")]
        public async Task<ActionResult<AdminCommentsResponseDto>> GetAllComments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "newest",
            [FromQuery] int? targetType = null,
            [FromQuery] bool showReported = false,
            [FromQuery] bool showDeleted = false, // ✅ NEW: Filter for deleted comments
            [FromQuery] string search = "")
        {
            try
            {
                _logger.LogInformation("Getting all comments for admin management - Page: {Page}, Size: {PageSize}", page, pageSize);

                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var query = _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.DeletedByUser) // ✅ NEW: Include who deleted it
                    .Include(c => c.Title)
                    .Include(c => c.Chapter)
                        .ThenInclude(ch => ch != null ? ch.Title : null)
                    .Include(c => c.ChapterImage)
                        .ThenInclude(ci => ci != null ? ci.Chapter : null)
                        .ThenInclude(ch => ch != null ? ch.Title : null)
                    .AsQueryable();

                // ✅ Filter by deletion status
                if (showDeleted)
                {
                    query = query.Where(c => c.IsDeleted);
                }
                else
                {
                    query = query.Where(c => !c.IsDeleted);
                }

                // Filter by target type if specified
                if (targetType.HasValue)
                {
                    switch (targetType.Value)
                    {
                        case 1: // Title comments
                            query = query.Where(c => c.TitleId != null);
                            break;
                        case 2: // Chapter comments
                            query = query.Where(c => c.ChapterId != null);
                            break;
                        case 3: // Chapter image comments
                            query = query.Where(c => c.ChapterImageId != null);
                            break;
                    }
                }

                // Filter reported comments
                if (showReported)
                {
                    query = query.Where(c => c.DislikesCount > c.LikesCount && c.DislikesCount > 5);
                }

                // Search functionality
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.ToLower();
                    query = query.Where(c =>
                        c.Content.ToLower().Contains(searchTerm) ||
                        (c.User != null && c.User.UserName.ToLower().Contains(searchTerm)) ||
                        (c.Title != null && c.Title.OriginalTitle.ToLower().Contains(searchTerm)) ||
                        (c.Chapter != null && c.Chapter.Title != null && c.Chapter.Title.OriginalTitle.ToLower().Contains(searchTerm))
                    );
                }

                // Apply sorting
                query = sortBy.ToLower() switch
                {
                    "oldest" => query.OrderBy(c => c.PostedDate),
                    "most_liked" => query.OrderByDescending(c => c.LikesCount),
                    "most_reported" => query.OrderByDescending(c => c.DislikesCount).ThenByDescending(c => c.PostedDate),
                    "recently_deleted" => query.OrderByDescending(c => c.DeletedAt), // ✅ NEW: Sort by deletion date
                    _ => query.OrderByDescending(c => c.PostedDate) // newest (default)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var comments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new AdminCommentDto
                    {
                        Id = c.Id,
                        Content = c.Content, // ✅ Show original content for admins
                        PostedDate = c.PostedDate,
                        UserId = c.UserId,
                        UserName = c.User != null ? c.User.UserName : "Unknown User",
                        LikesCount = c.LikesCount,
                        DislikesCount = c.DislikesCount,
                        ParentCommentId = c.ParentCommentId,
                        TitleId = c.TitleId,
                        ChapterId = c.ChapterId,
                        ChapterImageId = c.ChapterImageId,
                        TargetTitle = c.Title != null ? c.Title.OriginalTitle :
                                     c.Chapter != null && c.Chapter.Title != null ? c.Chapter.Title.OriginalTitle :
                                     c.ChapterImage != null && c.ChapterImage.Chapter != null && c.ChapterImage.Chapter.Title != null ? c.ChapterImage.Chapter.Title.OriginalTitle :
                                     "Unknown",
                        IsReported = c.DislikesCount > c.LikesCount && c.DislikesCount > 5,

                        // ✅ NEW: Soft delete fields
                        IsDeleted = c.IsDeleted,
                        DeletedAt = c.DeletedAt,
                        DeletedByUserId = c.DeletedByUserId,
                        DeletedByUserName = c.DeletedByUser != null ? c.DeletedByUser.UserName : null,
                        DeletionReason = c.DeletionReason
                    })
                    .ToListAsync();

                var result = new AdminCommentsResponseDto
                {
                    Comments = comments,
                    Pagination = new AdminPaginationDto
                    {
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = totalPages,
                        HasNext = page < totalPages,
                        HasPrevious = page > 1
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all comments for admin");
                return StatusCode(500, new { message = "Error retrieving comments", error = ex.Message });
            }
        }

        /// <summary>
        /// Get detailed comment information for admin
        /// GET: api/AdminComments/GetComment/{id}
        /// </summary>
        [HttpGet("GetComment/{id}")]
        public async Task<ActionResult<AdminCommentDetailDto>> GetComment(int id)
        {
            try
            {
                var comment = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Title)
                    .Include(c => c.Chapter)
                        .ThenInclude(ch => ch.Title)
                    .Include(c => c.ChapterImage)
                        .ThenInclude(ci => ci.Chapter)
                        .ThenInclude(ch => ch.Title)
                    .Include(c => c.Replies)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                var result = new AdminCommentDetailDto
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    PostedDate = comment.PostedDate,
                    UserId = comment.UserId,
                    UserName = comment.User?.UserName ?? "Unknown User",
                    LikesCount = comment.LikesCount,
                    DislikesCount = comment.DislikesCount,
                    ParentCommentId = comment.ParentCommentId,
                    TitleId = comment.TitleId,
                    ChapterId = comment.ChapterId,
                    ChapterImageId = comment.ChapterImageId,
                    TargetTitle = comment.Title?.OriginalTitle ??
                                 comment.Chapter?.Title?.OriginalTitle ??
                                 comment.ChapterImage?.Chapter?.Title?.OriginalTitle ??
                                 "Unknown",
                    TargetType = comment.TitleId != null ? "Title" :
                                comment.ChapterId != null ? "Chapter" :
                                comment.ChapterImageId != null ? "Image" : "Unknown",
                    Replies = comment.Replies?.Select(r => new AdminCommentDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        PostedDate = r.PostedDate,
                        UserId = r.UserId,
                        UserName = r.User?.UserName ?? "Unknown User",
                        LikesCount = r.LikesCount,
                        DislikesCount = r.DislikesCount,
                        ParentCommentId = r.ParentCommentId
                    }).ToList() ?? new List<AdminCommentDto>(),
                    IsReported = comment.DislikesCount > comment.LikesCount && comment.DislikesCount > 5
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment {CommentId}", id);
                return StatusCode(500, new { message = "Error retrieving comment", error = ex.Message });
            }
        }

        /// <summary>
        /// Soft delete a comment (admin) - marks as deleted instead of removing
        /// DELETE: api/AdminComments/DeleteComment/{id}
        /// </summary>
        [HttpDelete("DeleteComment/{id}")]
        public async Task<ActionResult> DeleteComment(int id, [FromQuery] string reason = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                if (comment.IsDeleted)
                {
                    return BadRequest(new { message = "Comment is already deleted" });
                }

                // ✅ Soft delete the comment
                comment.SoftDelete(user.Id, reason ?? "Deleted by administrator");

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} soft-deleted comment {CommentId}", user.Id, id);

                return Ok(new
                {
                    message = "Comment deleted successfully (soft delete)",
                    isDeleted = true,
                    deletedAt = comment.DeletedAt,
                    reason = comment.DeletionReason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting comment {CommentId}", id);
                return StatusCode(500, new { message = "Error deleting comment", error = ex.Message });
            }
        }

        /// <summary>
        /// Restore a soft-deleted comment (admin)
        /// POST: api/AdminComments/RestoreComment/{id}
        /// </summary>
        [HttpPost("RestoreComment/{id}")]
        public async Task<ActionResult> RestoreComment(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                if (!comment.IsDeleted)
                {
                    return BadRequest(new { message = "Comment is not deleted" });
                }

                // ✅ Restore the comment
                comment.Restore();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} restored comment {CommentId}", user.Id, id);

                return Ok(new
                {
                    message = "Comment restored successfully",
                    isDeleted = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring comment {CommentId}", id);
                return StatusCode(500, new { message = "Error restoring comment", error = ex.Message });
            }
        }

        /// <summary>
        /// Permanently delete a comment and all its replies (admin only, use with caution)
        /// DELETE: api/AdminComments/PermanentlyDeleteComment/{id}
        /// </summary>
        [HttpDelete("PermanentlyDeleteComment/{id}")]
        [Authorize(Roles = "Admin")] // Only admins can permanently delete
        public async Task<ActionResult> PermanentlyDeleteComment(int id, [FromQuery] bool confirmed = false)
        {
            try
            {
                if (!confirmed)
                {
                    return BadRequest(new
                    {
                        message = "Permanent deletion requires confirmation. Add ?confirmed=true to the request.",
                        warning = "This action cannot be undone!"
                    });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var comment = await _context.Comments
                    .Include(c => c.Replies)
                    .Include(c => c.Reactions)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Recursively delete all replies and their reactions (hard delete)
                    await PermanentlyDeleteCommentRecursively(comment);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogWarning("Admin {AdminId} PERMANENTLY deleted comment {CommentId} and all its replies", user.Id, id);

                    return Ok(new { message = "Comment permanently deleted (cannot be undone)" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during permanent comment deletion transaction for comment {CommentId}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting comment {CommentId}", id);
                return StatusCode(500, new { message = "Error permanently deleting comment", error = ex.Message });
            }
        }

        /// <summary>
        /// Bulk soft delete multiple comments
        /// POST: api/AdminComments/BulkDeleteComments
        /// </summary>
        [HttpPost("BulkDeleteComments")]
        public async Task<ActionResult> BulkDeleteComments([FromBody] BulkDeleteRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                if (request.CommentIds == null || !request.CommentIds.Any())
                {
                    return BadRequest(new { message = "No comment IDs provided" });
                }

                var comments = await _context.Comments
                    .Where(c => request.CommentIds.Contains(c.Id) && !c.IsDeleted)
                    .ToListAsync();

                if (!comments.Any())
                {
                    return BadRequest(new { message = "No valid comments found to delete" });
                }

                // Soft delete all comments
                foreach (var comment in comments)
                {
                    comment.SoftDelete(user.Id, request.Reason ?? "Bulk deleted by administrator");
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin {AdminId} bulk soft-deleted {Count} comments", user.Id, comments.Count);

                return Ok(new
                {
                    message = $"Successfully deleted {comments.Count} comments",
                    deletedCount = comments.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting comments");
                return StatusCode(500, new { message = "Error bulk deleting comments", error = ex.Message });
            }
        }

        /// <summary>
        /// Get comment deletion info (how many replies will be affected)
        /// GET: api/AdminComments/GetDeletionInfo/{id}
        /// </summary>
        [HttpGet("GetDeletionInfo/{id}")]
        public async Task<ActionResult<object>> GetCommentDeletionInfo(int id)
        {
            try
            {
                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                // Count total replies recursively
                var totalReplies = await CountRepliesRecursively(id);
                var directReplies = await _context.Comments.CountAsync(c => c.ParentCommentId == id);

                return Ok(new
                {
                    commentId = id,
                    hasReplies = totalReplies > 0,
                    directReplies = directReplies,
                    totalReplies = totalReplies,
                    warningMessage = totalReplies > 0
                        ? $"This will delete the comment and {totalReplies} reply(ies)"
                        : "This will delete only this comment"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deletion info for comment {CommentId}", id);
                return StatusCode(500, new { message = "Error retrieving deletion info", error = ex.Message });
            }
        }

        /// <summary>
        /// Count total replies recursively
        /// </summary>
        private async Task<int> CountRepliesRecursively(int commentId)
        {
            var directReplies = await _context.Comments
                .Where(c => c.ParentCommentId == commentId)
                .Select(c => c.Id)
                .ToListAsync();

            if (!directReplies.Any())
                return 0;

            int count = directReplies.Count;

            foreach (var replyId in directReplies)
            {
                count += await CountRepliesRecursively(replyId);
            }

            return count;
        }

        /// <summary>
        /// Recursively permanently delete comment and all its nested replies (HARD DELETE)
        /// </summary>
        private async Task PermanentlyDeleteCommentRecursively(Comment comment)
        {
            // First, load all direct replies with their own replies and reactions
            var directReplies = await _context.Comments
                .Include(c => c.Replies)
                .Include(c => c.Reactions)
                .Where(c => c.ParentCommentId == comment.Id)
                .ToListAsync();

            // Recursively delete all replies first (depth-first deletion)
            foreach (var reply in directReplies)
            {
                await PermanentlyDeleteCommentRecursively(reply);
            }

            // Delete reactions for this comment
            if (comment.Reactions != null && comment.Reactions.Any())
            {
                _context.CommentReactions.RemoveRange(comment.Reactions);
            }

            // Finally, delete the comment itself (HARD DELETE)
            _context.Comments.Remove(comment);
        }

        #region DTOs

        public class AdminCommentsResponseDto
        {
            public List<AdminCommentDto> Comments { get; set; } = new();
            public AdminPaginationDto Pagination { get; set; } = new();
        }

        public class AdminCommentDto
        {
            public int Id { get; set; }
            public string Content { get; set; } = string.Empty;
            public DateTime PostedDate { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public int LikesCount { get; set; }
            public int DislikesCount { get; set; }
            public int? ParentCommentId { get; set; }
            public int? TitleId { get; set; }
            public int? ChapterId { get; set; }
            public int? ChapterImageId { get; set; }
            public string TargetTitle { get; set; } = string.Empty;
            public bool IsReported { get; set; }

            // ✅ NEW: Soft delete fields
            public bool IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
            public string? DeletedByUserId { get; set; }
            public string? DeletedByUserName { get; set; }
            public string? DeletionReason { get; set; }
        }

        public class AdminCommentDetailDto : AdminCommentDto
        {
            public string TargetType { get; set; } = string.Empty;
            public List<AdminCommentDto> Replies { get; set; } = new();
        }

        public class AdminPaginationDto
        {
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
            public bool HasNext { get; set; }
            public bool HasPrevious { get; set; }
        }

        public class AdminCommentStatsDto
        {
            public int TotalComments { get; set; }
            public int DeletedComments { get; set; } // ✅ NEW
            public int TodayComments { get; set; }
            public int TodayDeleted { get; set; } // ✅ NEW
            public int ReportedComments { get; set; }
            public int ActiveCommenters { get; set; }
        }

        public class BulkDeleteRequest
        {
            public List<int> CommentIds { get; set; } = new();
            public string? Reason { get; set; }
        }

        #endregion
    }
}