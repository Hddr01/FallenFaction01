// Controllers/CommentsController.cs - Fixed for Infinite Accordion with Correct Schema
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

        /// <summary>
        /// Get comment statistics for a target
        /// GET: api/Comments/GetCommentStats
        /// </summary>
        [HttpGet("GetCommentStats")]
        public async Task<ActionResult<CommentStatsDto>> GetCommentStats(
            [FromQuery] int targetId,
            [FromQuery] int targetType)
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
        /// Get all comments for a target with infinite nesting support
        /// GET: api/Comments/GetComments
        /// No depth limit - supports infinite accordion expansion on frontend
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
                _logger.LogInformation(
                    "Getting comments for target {TargetType}:{TargetId}, page {Page}, sort {SortBy}",
                    targetType, targetId, page, sortBy);

                // Validate parameters
                if (targetType < 1 || targetType > 3)
                {
                    return BadRequest(new { message = "Invalid target type. Must be 1 (Title), 2 (Chapter), or 3 (ChapterImage)" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // Validate sort option
                var validSortOptions = new[] { "newest", "oldest", "likes" };
                if (!validSortOptions.Contains(sortBy.ToLower()))
                {
                    sortBy = "newest";
                }

                // Check if target exists and comments are enabled
                var statsResult = await GetCommentStats(targetId, targetType);
                if (statsResult.Result is not OkObjectResult statsOk)
                {
                    return statsResult.Result ?? NotFound(new { message = "Target not found" });
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
                var currentUserId = currentUser?.Id;
                var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

                // Build query based on target type (TitleId, ChapterId, or ChapterImageId)
                IQueryable<Comment> query = targetType switch
                {
                    1 => _context.Comments.Where(c => c.TitleId == targetId && c.ParentCommentId == null),
                    2 => _context.Comments.Where(c => c.ChapterId == targetId && c.ParentCommentId == null),
                    3 => _context.Comments.Where(c => c.ChapterImageId == targetId && c.ParentCommentId == null),
                    _ => throw new ArgumentException("Invalid target type")
                };

                query = query.Include(c => c.User);

                // Apply sorting
                query = sortBy.ToLower() switch
                {
                    "oldest" => query.OrderBy(c => c.PostedDate),
                    "likes" => query.OrderByDescending(c => c.Reactions.Count(r => r.IsLike)),
                    _ => query.OrderByDescending(c => c.PostedDate) // newest (default)
                };

                // Get total count
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Get paginated top-level comments
                var topLevelComments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Recursively load ALL nested replies (no depth limit)
                var commentDtos = new List<CommentDto>();
                foreach (var comment in topLevelComments)
                {
                    var dto = await BuildCommentDtoRecursive(comment, currentUserId, isAdmin);
                    commentDtos.Add(dto);
                }

                return Ok(new CommentsResponseDto
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
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for target {TargetType}:{TargetId}", targetType, targetId);
                return StatusCode(500, new { message = "An error occurred while loading comments" });
            }
        }

        /// <summary>
        /// Recursively build comment DTO with all nested replies (infinite depth)
        /// </summary>
        private async Task<CommentDto> BuildCommentDtoRecursive(
            Comment comment,
            string? currentUserId,
            bool isAdmin)
        {
            // Get user reactions for this comment
            var userReaction = currentUserId != null
                ? await _context.CommentReactions
                    .FirstOrDefaultAsync(r => r.CommentId == comment.Id && r.UserId == currentUserId)
                : null;

            var dto = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                UserId = comment.UserId,
                UserName = comment.IsDeleted ? null : comment.User?.UserName ?? "Unknown",
                UserAvatarUrl = comment.IsDeleted ? null : comment.User?.ProfilePicturePath, // Using ProfilePicturePath
                PostedDate = comment.PostedDate,
                LikesCount = comment.Reactions.Count(r => r.IsLike),
                DislikesCount = comment.Reactions.Count(r => !r.IsLike),
                CurrentUserLiked = userReaction?.IsLike ?? false,
                CurrentUserDisliked = userReaction != null && !userReaction.IsLike,
                IsDeleted = comment.IsDeleted,
                DeletedAt = comment.DeletedAt,
                DeletedByUserName = isAdmin ? comment.DeletedByUser?.UserName : null,
                DeletionReason = isAdmin ? comment.DeletionReason : null,
                ParentCommentId = comment.ParentCommentId,
                TitleId = comment.TitleId,
                ChapterId = comment.ChapterId,
                ChapterImageId = comment.ChapterImageId,
                Replies = new List<CommentDto>()
            };

            // Load all child comments (replies to this comment)
            var childComments = await _context.Comments
                .Where(c => c.ParentCommentId == comment.Id)
                .Include(c => c.User)
                .Include(c => c.Reactions)
                .Include(c => c.DeletedByUser)
                .OrderBy(c => c.PostedDate) // Replies sorted by oldest first (like Reddit)
                .ToListAsync();

            // Recursively build DTOs for each child comment (infinite nesting!)
            foreach (var child in childComments)
            {
                var childDto = await BuildCommentDtoRecursive(child, currentUserId, isAdmin);
                dto.Replies.Add(childDto);
            }

            return dto;
        }

        /// <summary>
        /// Add a new comment or reply (supports infinite nesting)
        /// POST: api/Comments/AddComment
        /// </summary>
        [Authorize]
        [HttpPost("AddComment")]
        public async Task<ActionResult<CommentDto>> AddComment([FromBody] AddCommentRequestDto dto)
        {
            try
            {
                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "You must be logged in to comment" });
                }

                // Validate target type
                if (dto.TargetType < 1 || dto.TargetType > 3)
                {
                    return BadRequest(new { message = "Invalid target type" });
                }

                // Validate content
                if (string.IsNullOrWhiteSpace(dto.Content))
                {
                    return BadRequest(new { message = "Comment content cannot be empty" });
                }

                if (dto.Content.Length > 2000)
                {
                    return BadRequest(new { message = "Comment cannot exceed 2000 characters" });
                }

                // Check if target exists and comments are enabled
                var statsResult = await GetCommentStats(dto.TargetId, dto.TargetType);
                if (statsResult.Result is not OkObjectResult statsOk)
                {
                    return BadRequest(new { message = "Target not found or comments are disabled" });
                }

                var stats = (CommentStatsDto)statsOk.Value!;
                if (!stats.CommentsEnabled)
                {
                    return BadRequest(new { message = "Comments are disabled for this content" });
                }

                // If this is a reply, verify parent comment exists
                if (dto.ParentCommentId.HasValue)
                {
                    var parentExists = await _context.Comments
                        .AnyAsync(c => c.Id == dto.ParentCommentId.Value);

                    if (!parentExists)
                    {
                        return BadRequest(new { message = "Parent comment not found" });
                    }

                    // No depth limit check! Infinite nesting is allowed
                }

                // Create new comment with correct foreign key based on target type
                var comment = new Comment
                {
                    Content = dto.Content.Trim(),
                    UserId = currentUser.Id,
                    ParentCommentId = dto.ParentCommentId,
                    PostedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                // Set the correct foreign key based on target type
                switch (dto.TargetType)
                {
                    case 1: // Title
                        comment.TitleId = dto.TargetId;
                        break;
                    case 2: // Chapter
                        comment.ChapterId = dto.TargetId;
                        break;
                    case 3: // ChapterImage
                        comment.ChapterImageId = dto.TargetId;
                        break;
                }

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Reload comment with user info
                var createdComment = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Reactions)
                    .FirstOrDefaultAsync(c => c.Id == comment.Id);

                if (createdComment == null)
                {
                    return StatusCode(500, new { message = "Failed to retrieve created comment" });
                }

                // Build DTO (no replies for new comment)
                var commentDto = new CommentDto
                {
                    Id = createdComment.Id,
                    Content = createdComment.Content,
                    UserId = createdComment.UserId,
                    UserName = createdComment.User?.UserName ?? "Unknown",
                    UserAvatarUrl = createdComment.User?.ProfilePicturePath,
                    PostedDate = createdComment.PostedDate,
                    LikesCount = 0,
                    DislikesCount = 0,
                    CurrentUserLiked = false,
                    CurrentUserDisliked = false,
                    IsDeleted = false,
                    ParentCommentId = createdComment.ParentCommentId,
                    TitleId = createdComment.TitleId,
                    ChapterId = createdComment.ChapterId,
                    ChapterImageId = createdComment.ChapterImageId,
                    Replies = new List<CommentDto>()
                };

                _logger.LogInformation(
                    "User {UserId} added comment {CommentId} on {TargetType}:{TargetId}",
                    currentUser.Id, comment.Id, dto.TargetType, dto.TargetId);

                return CreatedAtAction(nameof(AddComment), new { id = commentDto.Id }, commentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                return StatusCode(500, new { message = "An error occurred while adding the comment" });
            }
        }

        /// <summary>
        /// Toggle like/dislike on a comment
        /// POST: api/Comments/{commentId}/React
        /// </summary>
        [Authorize]
        [HttpPost("{commentId}/React")]
        public async Task<ActionResult<CommentReactionResponseDto>> ReactToComment(
            int commentId,
            [FromBody] CommentReactionRequestDto request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "You must be logged in to react to comments" });
                }

                var comment = await _context.Comments
                    .Include(c => c.Reactions)
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                if (comment.IsDeleted)
                {
                    return BadRequest(new { message = "Cannot react to deleted comments" });
                }

                // Find existing reaction
                var existingReaction = await _context.CommentReactions
                    .FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == currentUser.Id);

                bool userLiked = false;
                bool userDisliked = false;

                if (existingReaction != null)
                {
                    // If clicking the same reaction, remove it (toggle off)
                    if (existingReaction.IsLike == request.IsLike)
                    {
                        _context.CommentReactions.Remove(existingReaction);
                    }
                    else
                    {
                        // Switch to opposite reaction
                        existingReaction.IsLike = request.IsLike;
                        userLiked = request.IsLike;
                        userDisliked = !request.IsLike;
                    }
                }
                else
                {
                    // Add new reaction
                    var reaction = new CommentReaction
                    {
                        CommentId = commentId,
                        UserId = currentUser.Id,
                        IsLike = request.IsLike
                    };
                    _context.CommentReactions.Add(reaction);
                    userLiked = request.IsLike;
                    userDisliked = !request.IsLike;
                }

                await _context.SaveChangesAsync();

                // Reload to get updated counts
                var updatedComment = await _context.Comments
                    .Include(c => c.Reactions)
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                return Ok(new CommentReactionResponseDto
                {
                    CommentId = commentId,
                    LikesCount = updatedComment!.Reactions.Count(r => r.IsLike),
                    DislikesCount = updatedComment.Reactions.Count(r => !r.IsLike),
                    UserLiked = userLiked,
                    UserDisliked = userDisliked,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to comment {CommentId}", commentId);
                return StatusCode(500, new { message = "An error occurred while processing your reaction" });
            }
        }

        /// <summary>
        /// Delete own comment (hard delete) or soft delete as admin
        /// DELETE: api/Comments/{commentId}
        /// </summary>
        [Authorize]
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "You must be logged in to delete comments" });
                }

                var comment = await _context.Comments
                    .Include(c => c.Replies)
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                var isOwner = comment.UserId == currentUser.Id;

                if (!isOwner && !isAdmin)
                {
                    return Forbid();
                }

                if (isAdmin)
                {
                    // Admin performs soft delete using helper method
                    comment.SoftDelete(currentUser.Id, "Deleted by administrator");

                    // Recursively soft delete all replies
                    await SoftDeleteRepliesRecursive(comment, currentUser.Id);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Admin {UserId} soft deleted comment {CommentId}",
                        currentUser.Id, commentId);

                    return Ok(new { message = "Comment deleted successfully (soft delete)" });
                }
                else
                {
                    // Owner performs hard delete (only if no replies exist)
                    if (comment.Replies.Any())
                    {
                        return BadRequest(new
                        {
                            message = "Cannot delete comment with replies. Please contact an administrator."
                        });
                    }

                    // Remove reactions first
                    var reactions = await _context.CommentReactions
                        .Where(r => r.CommentId == commentId)
                        .ToListAsync();
                    _context.CommentReactions.RemoveRange(reactions);

                    // Delete comment
                    _context.Comments.Remove(comment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "User {UserId} deleted own comment {CommentId}",
                        currentUser.Id, commentId);

                    return Ok(new { message = "Comment deleted successfully" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return StatusCode(500, new { message = "An error occurred while deleting the comment" });
            }
        }

        /// <summary>
        /// Recursively soft delete all replies to a comment
        /// </summary>
        private async Task SoftDeleteRepliesRecursive(Comment parentComment, string deletedByUserId)
        {
            var replies = await _context.Comments
                .Where(c => c.ParentCommentId == parentComment.Id)
                .ToListAsync();

            foreach (var reply in replies)
            {
                if (!reply.IsDeleted)
                {
                    reply.SoftDelete(deletedByUserId, "Parent comment was deleted");

                    // Recursively delete this reply's replies
                    await SoftDeleteRepliesRecursive(reply, deletedByUserId);
                }
            }
        }

        /// <summary>
        /// Restore soft-deleted comment (Admin only)
        /// POST: api/Comments/{commentId}/Restore
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{commentId}/Restore")]
        public async Task<IActionResult> RestoreComment(int commentId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                if (!comment.IsDeleted)
                {
                    return BadRequest(new { message = "Comment is not deleted" });
                }

                // Use helper method to restore
                comment.Restore();

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin {UserId} restored comment {CommentId}",
                    currentUser.Id, commentId);

                return Ok(new { message = "Comment restored successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring comment {CommentId}", commentId);
                return StatusCode(500, new { message = "An error occurred while restoring the comment" });
            }
        }




        [HttpGet("GetCommentThread/{commentId}")]
        public async Task<ActionResult<CommentThreadResponseDto>> GetCommentThread(int commentId)
        {
            try
            {
                _logger.LogInformation("Getting comment thread for comment {CommentId}", commentId);

                // Get current user for permission checks
                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserId = currentUser?.Id;
                var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

                // Get the requested comment
                var comment = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Reactions)
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                // Build the comment DTO with all its replies recursively
                var commentDto = await BuildCommentDtoRecursive(comment, currentUserId, isAdmin);

                // Get parent comments chain for breadcrumb navigation
                var parentChain = new List<CommentBreadcrumbDto>();
                var currentParentId = comment.ParentCommentId;

                while (currentParentId.HasValue)
                {
                    var parentComment = await _context.Comments
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.Id == currentParentId.Value);

                    if (parentComment == null) break;

                    parentChain.Insert(0, new CommentBreadcrumbDto
                    {
                        Id = parentComment.Id,
                        UserName = parentComment.User?.UserName ?? "Unknown",
                        Content = parentComment.Content.Length > 100
                            ? parentComment.Content.Substring(0, 100) + "..."
                            : parentComment.Content,
                        IsDeleted = parentComment.IsDeleted
                    });

                    currentParentId = parentComment.ParentCommentId;
                }

                return Ok(new CommentThreadResponseDto
                {
                    Comment = commentDto,
                    ParentChain = parentChain,
                    TargetId = comment.TitleId ?? comment.ChapterId ?? comment.ChapterImageId ?? 0,
                    TargetType = comment.TitleId.HasValue ? 1 : comment.ChapterId.HasValue ? 2 : 3
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment thread for comment {CommentId}", commentId);
                return StatusCode(500, new { message = "An error occurred while loading the comment thread" });
            }
        }
    }
    public class CommentThreadResponseDto
    {
        public CommentDto Comment { get; set; }
        public List<CommentBreadcrumbDto> ParentChain { get; set; }
        public int TargetId { get; set; }
        public int TargetType { get; set; }
    }

    public class CommentBreadcrumbDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Content { get; set; }
        public bool IsDeleted { get; set; }
    }
}