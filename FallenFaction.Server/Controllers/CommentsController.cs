// Controllers/CommentsController.cs - Fixed to show soft deleted comments as [Deleted]
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CommentsController> _logger;
        private readonly ICommentService _commentService;

        public CommentsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<CommentsController> logger,
            ICommentService commentService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _commentService = commentService;
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
        /// Get comments for a target with pagination and sorting (includes soft deleted comments marked as [Deleted])
        /// GET: api/Comments/GetComments
        /// </summary>
        [HttpGet("GetComments")]
        public async Task<ActionResult<CommentsResponseDto>> GetComments(
            [FromQuery] int targetId,
            [FromQuery] int targetType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "newest")
        {
            try
            {
                _logger.LogInformation("Getting comments for target {TargetType}:{TargetId}, page {Page}", targetType, targetId, page);

                // Validate parameters
                if (targetType < 1 || targetType > 3)
                {
                    return BadRequest(new { message = "Invalid target type" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // Check if target exists and comments are enabled
                var statsResult = await GetCommentStats(targetId, targetType);
                if (statsResult.Result is not OkObjectResult statsOk)
                {
                    return statsResult.Result ?? NotFound();
                }

                var stats = (CommentStatsDto)statsOk.Value!;
                if (!stats.CommentsEnabled)
                {
                    return Ok(new CommentsResponseDto
                    {
                        Comments = new List<CommentDto>(),
                        Pagination = new PaginationDto
                        {
                            TotalCount = 0,
                            Page = page,
                            PageSize = pageSize,
                            TotalPages = 0,
                            HasNext = false,
                            HasPrevious = false
                        }
                    });
                }

                // Get current user for permission checks and reaction status
                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

                // Get comments with pagination - ALWAYS include soft deleted comments
                var query = GetCommentsQuery(targetId, targetType);

                // Apply sorting
                query = sortBy.ToLower() switch
                {
                    "oldest" => query.OrderBy(c => c.PostedDate),
                    "likes" => query.OrderByDescending(c => c.LikesCount).ThenByDescending(c => c.PostedDate),
                    _ => query.OrderByDescending(c => c.PostedDate) // newest (default)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var comments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(c => c.DeletedByUser)
                    .Include(c => c.Replies).ThenInclude(r => r.DeletedByUser)
                    .Include(c => c.Reactions) // Include reactions for current user status
                    .Include(c => c.Replies).ThenInclude(r => r.Reactions)
                    .ToListAsync();

                var commentDtos = comments.Select(c => MapToCommentDto(c, currentUser?.Id, isAdmin)).ToList();

                // Set pagination headers
                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                var result = new CommentsResponseDto
                {
                    Comments = commentDtos,
                    Pagination = new PaginationDto
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
                _logger.LogError(ex, "Error getting comments for target {TargetType}:{TargetId}", targetType, targetId);
                return StatusCode(500, new { message = "Error retrieving comments", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new comment
        /// POST: api/Comments/AddComment
        /// </summary>
        [HttpPost("AddComment")]
        [Authorize]
        public async Task<ActionResult<CommentDto>> AddComment([FromBody] AddCommentRequestDto request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { message = "Comment content is required" });
                }

                if (request.Content.Length > 2000)
                {
                    return BadRequest(new { message = "Comment is too long (maximum 2000 characters)" });
                }

                if (request.TargetType < 1 || request.TargetType > 3)
                {
                    return BadRequest(new { message = "Invalid target type" });
                }

                // Check if target exists and comments are enabled
                var statsResult = await GetCommentStats(request.TargetId, request.TargetType);
                if (statsResult.Result is not OkObjectResult statsOk)
                {
                    return BadRequest(new { message = "Invalid target or comments disabled" });
                }

                var stats = (CommentStatsDto)statsOk.Value!;
                if (!stats.CommentsEnabled)
                {
                    return BadRequest(new { message = "Comments are disabled for this content" });
                }

                // Validate parent comment if this is a reply
                if (request.ParentCommentId.HasValue)
                {
                    var parentComment = await _context.Comments
                        .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId.Value);

                    if (parentComment == null)
                    {
                        return BadRequest(new { message = "Parent comment not found" });
                    }

                    // Allow replies to soft deleted comments (they'll show under [Deleted] parent)

                    // Ensure parent comment belongs to the same target
                    bool parentMatches = request.TargetType switch
                    {
                        1 => parentComment.TitleId == request.TargetId,
                        2 => parentComment.ChapterId == request.TargetId,
                        3 => parentComment.ChapterImageId == request.TargetId,
                        _ => false
                    };

                    if (!parentMatches)
                    {
                        return BadRequest(new { message = "Parent comment does not belong to the same target" });
                    }
                }

                // Create new comment
                var comment = new Comment
                {
                    Content = request.Content.Trim(),
                    UserId = user.Id,
                    User = user,
                    PostedDate = DateTime.UtcNow,
                    ParentCommentId = request.ParentCommentId,
                    LikesCount = 0,
                    DislikesCount = 0,
                    IsDeleted = false
                };

                // Set target based on type
                switch (request.TargetType)
                {
                    case 1:
                        comment.TitleId = request.TargetId;
                        break;
                    case 2:
                        comment.ChapterId = request.TargetId;
                        break;
                    case 3:
                        comment.ChapterImageId = request.TargetId;
                        break;
                }

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} added comment {CommentId} to target {TargetType}:{TargetId}",
                    user.Id, comment.Id, request.TargetType, request.TargetId);

                // Return the created comment as DTO
                var commentDto = new CommentDto
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    PostedDate = comment.PostedDate,
                    UserId = comment.UserId,
                    UserName = user.UserName ?? "Unknown User",
                    UserAvatarUrl = user.ProfilePicturePath,
                    LikesCount = comment.LikesCount,
                    DislikesCount = comment.DislikesCount,
                    CurrentUserLiked = false,
                    CurrentUserDisliked = false,
                    ParentCommentId = comment.ParentCommentId,
                    TitleId = comment.TitleId,
                    ChapterId = comment.ChapterId,
                    ChapterImageId = comment.ChapterImageId,
                    IsDeleted = false,
                    Replies = new List<CommentDto>()
                };

                return CreatedAtAction(nameof(GetComments),
                    new { targetId = request.TargetId, targetType = request.TargetType },
                    commentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                return StatusCode(500, new { message = "Error adding comment", error = ex.Message });
            }
        }

        /// <summary>
        /// React to a comment (like or dislike)
        /// POST: api/Comments/ReactToComment
        /// </summary>
        [HttpPost("ReactToComment")]
        [Authorize]
        public async Task<ActionResult<CommentReactionResponseDto>> ReactToComment([FromBody] CommentReactionRequestDto request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var comment = await _context.Comments
                    .Include(c => c.Reactions)
                    .FirstOrDefaultAsync(c => c.Id == request.CommentId && !c.IsDeleted);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found or deleted" });
                }

                // Check if user already has a reaction for this comment
                var existingReaction = comment.Reactions?.FirstOrDefault(r => r.UserId == user.Id);

                if (existingReaction != null)
                {
                    // Update existing reaction
                    if (existingReaction.IsLike == request.IsLike)
                    {
                        // Same reaction - remove it (toggle off)
                        _context.CommentReactions.Remove(existingReaction);

                        if (request.IsLike)
                            comment.LikesCount = Math.Max(0, comment.LikesCount - 1);
                        else
                            comment.DislikesCount = Math.Max(0, comment.DislikesCount - 1);
                    }
                    else
                    {
                        // Different reaction - update it
                        existingReaction.IsLike = request.IsLike;
                        existingReaction.CreatedAt = DateTime.UtcNow; // Use CreatedAt instead of ReactedAt

                        if (request.IsLike)
                        {
                            comment.LikesCount += 1;
                            comment.DislikesCount = Math.Max(0, comment.DislikesCount - 1);
                        }
                        else
                        {
                            comment.DislikesCount += 1;
                            comment.LikesCount = Math.Max(0, comment.LikesCount - 1);
                        }
                    }
                }
                else
                {
                    // Create new reaction
                    var newReaction = new CommentReaction
                    {
                        CommentId = request.CommentId,
                        UserId = user.Id,
                        IsLike = request.IsLike,
                        CreatedAt = DateTime.UtcNow // Use CreatedAt instead of ReactedAt
                    };

                    _context.CommentReactions.Add(newReaction);

                    if (request.IsLike)
                        comment.LikesCount += 1;
                    else
                        comment.DislikesCount += 1;
                }

                await _context.SaveChangesAsync();

                // Get updated reaction status
                var userReaction = await _context.CommentReactions
                    .FirstOrDefaultAsync(r => r.CommentId == request.CommentId && r.UserId == user.Id);

                var result = new CommentReactionResponseDto
                {
                    CommentId = request.CommentId,
                    LikesCount = comment.LikesCount,
                    DislikesCount = comment.DislikesCount,
                    UserLiked = userReaction?.IsLike == true,
                    UserDisliked = userReaction?.IsLike == false,
                    Success = true
                };

                _logger.LogInformation("User {UserId} reacted to comment {CommentId} with {Reaction}",
                    user.Id, request.CommentId, request.IsLike ? "like" : "dislike");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to comment {CommentId}", request.CommentId);

                var errorResult = new CommentReactionResponseDto
                {
                    CommentId = request.CommentId,
                    Success = false,
                    Error = "Error processing reaction"
                };

                return StatusCode(500, errorResult);
            }
        }

        /// <summary>
        /// Soft delete a comment - marks as deleted instead of removing
        /// DELETE: api/Comments/DeleteComment/{id}
        /// </summary>
        [HttpDelete("DeleteComment/{id}")]
        [Authorize]
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

                // Check if user can delete (own comment or admin/moderator)
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var isModerator = await _userManager.IsInRoleAsync(user, "Moderator");

                if (comment.UserId != user.Id && !isAdmin && !isModerator)
                {
                    return Forbid("You can only delete your own comments");
                }

                // Soft delete the comment
                comment.SoftDelete(user.Id, reason ?? "Deleted by user");

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} soft-deleted comment {CommentId}", user.Id, id);

                return Ok(new
                {
                    message = "Comment deleted successfully",
                    isDeleted = true,
                    deletedAt = comment.DeletedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting comment {CommentId}", id);
                return StatusCode(500, new { message = "Error deleting comment", error = ex.Message });
            }
        }

        /// <summary>
        /// Restore a soft-deleted comment (Admin/Moderator only)
        /// POST: api/Comments/RestoreComment/{id}
        /// </summary>
        [HttpPost("RestoreComment/{id}")]
        [Authorize(Roles = "Admin,Moderator")]
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

                // Restore the comment
                comment.Restore();

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} restored comment {CommentId}", user.Id, id);

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

        #region Helper Methods

        /// <summary>
        /// Map Comment entity to CommentDto with user reaction status and proper soft delete handling
        /// </summary>
        private CommentDto MapToCommentDto(Comment comment, string currentUserId, bool isAdmin)
        {
            // Get user's reaction status (only for non-deleted comments)
            var userReaction = !comment.IsDeleted ? comment.Reactions?.FirstOrDefault(r => r.UserId == currentUserId) : null;

            var dto = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content, // Always include original content
                PostedDate = comment.PostedDate,
                UserId = comment.UserId,
                UserName = comment.User.UserName ?? "Unknown User",
                UserAvatarUrl = comment.User.ProfilePicturePath,
                LikesCount = comment.LikesCount,
                DislikesCount = comment.DislikesCount,
                CurrentUserLiked = userReaction?.IsLike == true,
                CurrentUserDisliked = userReaction?.IsLike == false,
                ParentCommentId = comment.ParentCommentId,
                TitleId = comment.TitleId,
                ChapterId = comment.ChapterId,
                ChapterImageId = comment.ChapterImageId,

                // ✅ NEW: Include soft delete information
                IsDeleted = comment.IsDeleted,
                DeletedAt = comment.DeletedAt,
                DeletedByUserName = comment.DeletedByUser?.UserName,
                DeletionReason = comment.DeletionReason,

                // Always include replies (both deleted and non-deleted)
                Replies = comment.Replies
                    ?.Select(r => MapToCommentDto(r, currentUserId, isAdmin))
                    ?.ToList() ?? new List<CommentDto>()
            };

            return dto;
        }

        /// <summary>
        /// Get comments query - ALWAYS includes soft deleted comments (they'll be marked as deleted in UI)
        /// </summary>
        private IQueryable<Comment> GetCommentsQuery(int targetId, int targetType)
        {
            var query = _context.Comments
                .Include(c => c.User)
                .Include(c => c.DeletedByUser)
                .Include(c => c.Replies) // Include ALL replies (deleted and non-deleted)
                .ThenInclude(r => r.User)
                .Include(c => c.Replies)
                .ThenInclude(r => r.DeletedByUser)
                .Where(c => c.ParentCommentId == null); // Only top-level comments

            // ✅ REMOVED: No longer filter out deleted comments - they'll be shown as [Deleted]
            // The frontend will handle displaying them appropriately

            // Filter by target type
            query = targetType switch
            {
                1 => query.Where(c => c.TitleId == targetId),
                2 => query.Where(c => c.ChapterId == targetId),
                3 => query.Where(c => c.ChapterImageId == targetId),
                _ => query.Where(c => false) // No matches for invalid type
            };

            return query;
        }

        #endregion
    }
}