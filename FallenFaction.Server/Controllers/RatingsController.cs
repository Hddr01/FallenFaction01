// Controllers/RatingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Ratings;

namespace FallenFaction.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<RatingsController> _logger;

        public RatingsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<RatingsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all ratings for a specific title with pagination and sorting
        /// GET: api/Ratings/GetRatings?titleId={id}&page={page}&pageSize={size}&sortBy={sort}
        /// </summary>
        [HttpGet("GetRatings")]
        public async Task<ActionResult<IEnumerable<RatingDto>>> GetRatings([FromQuery] GetRatingsRequest request)
        {
            try
            {
                _logger.LogInformation("Getting ratings for title {TitleId}", request.TitleId);

                // Verify title exists
                var titleExists = await _context.Titles.AnyAsync(t => t.Id == request.TitleId);
                if (!titleExists)
                {
                    return NotFound(new { message = "Title not found" });
                }

                // Build the base query
                var baseQuery = _context.Ratings
                    .Where(r => r.TitleId == request.TitleId);

                // Apply sorting
                var sortedQuery = request.SortBy?.ToLower() switch
                {
                    "oldest" => baseQuery.OrderBy(r => r.CreatedAt),
                    "highest" => baseQuery.OrderByDescending(r => r.Value).ThenByDescending(r => r.CreatedAt),
                    "lowest" => baseQuery.OrderBy(r => r.Value).ThenByDescending(r => r.CreatedAt),
                    _ => baseQuery.OrderByDescending(r => r.CreatedAt) // newest (default)
                };

                // Get total count for pagination
                var totalCount = await baseQuery.CountAsync();

                // Apply pagination and include user data
                var ratings = await sortedQuery
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Include(r => r.User)
                    .Select(r => new RatingDto
                    {
                        Id = r.Id,
                        Value = r.Value,
                        TitleId = r.TitleId,
                        UserId = r.UserId,
                        UserName = r.User.ProfileName ?? r.User.UserName ?? "Anonymous",
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", request.Page.ToString());
                Response.Headers.Add("X-Page-Size", request.PageSize.ToString());

                return Ok(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ratings for title {TitleId}", request.TitleId);
                return StatusCode(500, new { message = "Error retrieving ratings" });
            }
        }

        /// <summary>
        /// Add a new rating for a title
        /// POST: api/Ratings/AddRating
        /// </summary>
        [HttpPost("AddRating")]
        [Authorize]
        public async Task<ActionResult<RatingsSummaryDto>> AddRating([FromBody] AddRatingRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Verify title exists
                var title = await _context.Titles.FirstOrDefaultAsync(t => t.Id == request.TitleId);
                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                // Check if user already rated this title
                var existingRating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.TitleId == request.TitleId && r.UserId == user.Id);

                if (existingRating != null)
                {
                    return BadRequest(new { message = "You have already rated this title. Use UpdateRating to change your rating." });
                }

                // Create new rating
                var rating = new Rating
                {
                    Value = request.Value,
                    TitleId = request.TitleId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Ratings.Add(rating);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} added rating {Value} for title {TitleId}",
                    user.Id, request.Value, request.TitleId);

                // Award XP for rating a title (first time only, handled by duplicate check above)
                await AwardXpAsync(user.Id, 5, "Rated a title");
                await _context.SaveChangesAsync();

                // Return updated statistics
                var stats = await GetRatingStatistics(request.TitleId, user.Id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding rating for title {TitleId}", request.TitleId);
                return StatusCode(500, new { message = "Error adding rating" });
            }
        }

        /// <summary>
        /// Update an existing rating
        /// PUT: api/Ratings/UpdateRating/{ratingId}
        /// </summary>
        [HttpPut("UpdateRating/{ratingId}")]
        [Authorize]
        public async Task<ActionResult<RatingsSummaryDto>> UpdateRating(int ratingId, [FromBody] UpdateRatingRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (ratingId != request.RatingId)
            {
                return BadRequest(new { message = "Rating ID mismatch" });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var rating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.Id == ratingId);

                if (rating == null)
                {
                    return NotFound(new { message = "Rating not found" });
                }

                // Verify user owns this rating
                if (rating.UserId != user.Id)
                {
                    return Forbid("You can only update your own ratings");
                }

                var oldValue = rating.Value;
                rating.Value = request.Value;
                rating.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated rating {RatingId} from {OldValue} to {NewValue}",
                    user.Id, ratingId, oldValue, request.Value);

                // Return updated statistics
                var stats = await GetRatingStatistics(rating.TitleId, user.Id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating {RatingId}", ratingId);
                return StatusCode(500, new { message = "Error updating rating" });
            }
        }

        /// <summary>
        /// Delete a rating
        /// DELETE: api/Ratings/DeleteRating/{ratingId}
        /// </summary>
        [HttpDelete("DeleteRating/{ratingId}")]
        [Authorize]
        public async Task<ActionResult<RatingsSummaryDto>> DeleteRating(int ratingId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var rating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.Id == ratingId);

                if (rating == null)
                {
                    return NotFound(new { message = "Rating not found" });
                }

                // Verify user owns this rating or is admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (rating.UserId != user.Id && !isAdmin)
                {
                    return Forbid("You can only delete your own ratings");
                }

                var titleId = rating.TitleId;
                _context.Ratings.Remove(rating);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted rating {RatingId} for title {TitleId}",
                    user.Id, ratingId, titleId);

                // Return updated statistics
                var stats = await GetRatingStatistics(titleId, user.Id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rating {RatingId}", ratingId);
                return StatusCode(500, new { message = "Error deleting rating" });
            }
        }

        /// <summary>
        /// Get current user's rating for a title
        /// GET: api/Ratings/GetUserRating?titleId={id}
        /// </summary>
        [HttpGet("GetUserRating")]
        [Authorize]
        public async Task<ActionResult<UserRatingDto>> GetUserRating([FromQuery] GetUserRatingRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Verify title exists
                var titleExists = await _context.Titles.AnyAsync(t => t.Id == request.TitleId);
                if (!titleExists)
                {
                    return NotFound(new { message = "Title not found" });
                }

                var rating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.TitleId == request.TitleId && r.UserId == user.Id);

                var userRating = new UserRatingDto
                {
                    RatingId = rating?.Id,
                    Value = rating?.Value,
                    HasRated = rating != null,
                    RatedAt = rating?.UpdatedAt
                };

                return Ok(userRating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user rating for title {TitleId}", request.TitleId);
                return StatusCode(500, new { message = "Error retrieving user rating" });
            }
        }

        /// <summary>
        /// Get rating statistics for a title
        /// GET: api/Ratings/GetRatingStats?titleId={id}
        /// </summary>
        [HttpGet("GetRatingStats")]
        public async Task<ActionResult<RatingStatsDto>> GetRatingStats([FromQuery] GetRatingStatsRequest request)
        {
            try
            {
                _logger.LogInformation("Getting rating statistics for title {TitleId}", request.TitleId);

                // Verify title exists
                var titleExists = await _context.Titles.AnyAsync(t => t.Id == request.TitleId);
                if (!titleExists)
                {
                    return NotFound(new { message = "Title not found" });
                }

                var ratings = await _context.Ratings
                    .Where(r => r.TitleId == request.TitleId)
                    .Select(r => r.Value)
                    .ToListAsync();

                var stats = new RatingStatsDto
                {
                    TitleId = request.TitleId,
                    Total = ratings.Count,
                    Average = ratings.Any() ? Math.Round(ratings.Average(), 2) : 0.0,
                    Distribution = new List<RatingDistributionDto>()
                };

                if (ratings.Any())
                {
                    // Calculate distribution for ratings 1-10
                    for (int i = 1; i <= 10; i++)
                    {
                        var count = ratings.Count(r => r == i);
                        var percentage = count > 0 ? Math.Round((double)count / ratings.Count * 100, 1) : 0.0;

                        stats.Distribution.Add(new RatingDistributionDto
                        {
                            Value = i,
                            Count = count,
                            Percentage = percentage
                        });
                    }
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rating statistics for title {TitleId}", request.TitleId);
                return StatusCode(500, new { message = "Error retrieving rating statistics" });
            }
        }

        /// <summary>
        /// Get comprehensive rating summary for a title (includes user's rating if authenticated)
        /// GET: api/Ratings/GetRatingSummary?titleId={id}
        /// </summary>
        [HttpGet("GetRatingSummary")]
        public async Task<ActionResult<RatingsSummaryDto>> GetRatingSummary([FromQuery] int titleId)
        {
            try
            {
                _logger.LogInformation("Getting rating summary for title {TitleId}", titleId);

                // Get title info
                var title = await _context.Titles
                    .FirstOrDefaultAsync(t => t.Id == titleId);

                if (title == null)
                {
                    return NotFound(new { message = "Title not found" });
                }

                // Get current user if authenticated
                AppUser? user = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    user = await _userManager.GetUserAsync(User);
                }

                var stats = await GetRatingStatistics(titleId, user?.Id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rating summary for title {TitleId}", titleId);
                return StatusCode(500, new { message = "Error retrieving rating summary" });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// GET: api/Ratings/health
        /// </summary>
        [HttpGet("health")]
        public ActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "RatingsController"
            });
        }

        #region Private Methods

        private async Task<RatingsSummaryDto> GetRatingStatistics(int titleId, string? userId = null)
        {
            var title = await _context.Titles
                .FirstOrDefaultAsync(t => t.Id == titleId);

            var ratings = await _context.Ratings
                .Where(r => r.TitleId == titleId)
                .Select(r => r.Value)
                .ToListAsync();

            UserRatingDto? userRating = null;
            if (!string.IsNullOrEmpty(userId))
            {
                var rating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.TitleId == titleId && r.UserId == userId);

                userRating = new UserRatingDto
                {
                    RatingId = rating?.Id,
                    Value = rating?.Value,
                    HasRated = rating != null,
                    RatedAt = rating?.UpdatedAt
                };
            }

            var summary = new RatingsSummaryDto
            {
                TitleId = titleId,
                TitleName = title?.EnglishTitle ?? title?.OriginalTitle ?? "Unknown",
                TotalRatings = ratings.Count,
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(), 2) : 0.0,
                UserRating = userRating,
                Distribution = new List<RatingDistributionDto>()
            };

            if (ratings.Any())
            {
                // Calculate distribution for ratings 1-10
                for (int i = 1; i <= 10; i++)
                {
                    var count = ratings.Count(r => r == i);
                    var percentage = count > 0 ? Math.Round((double)count / ratings.Count * 100, 1) : 0.0;

                    summary.Distribution.Add(new RatingDistributionDto
                    {
                        Value = i,
                        Count = count,
                        Percentage = percentage
                    });
                }
            }

            return summary;
        }

        #endregion

        // ── XP helper ────────────────────────────────────────────────────────
        private async Task AwardXpAsync(string userId, int xpAmount, string reason)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;
            user.XpPoints += xpAmount;
            user.UserLevel = AppUser.ComputeLevel(user.XpPoints);
            _logger.LogDebug("XP +{Xp} → {UserId} ({Reason})", xpAmount, userId, reason);
        }
    }
}