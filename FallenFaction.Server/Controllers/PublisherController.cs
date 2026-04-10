// Controllers/PublisherController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Publisher;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PublisherController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<PublisherController> _logger;

        public PublisherController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<PublisherController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/publisher
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PublisherListDto>>> GetPublishers()
        {
            try
            {
                var publishers = await _context.Publishers
                    .Include(p => p.Titles)
                    .Select(p => new PublisherListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        TitleCount = p.Titles.Count,
                        CreatedDate = DateTime.UtcNow // You might want to add CreatedDate to Publisher model
                    })
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return Ok(publishers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving publishers");
                return StatusCode(500, new { message = "An error occurred while retrieving publishers" });
            }
        }

        // GET: api/publisher/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<PublisherDto>> GetPublisher(int id)
        {
            try
            {
                var publisher = await _context.Publishers
                    .Include(p => p.Titles)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (publisher == null)
                {
                    return NotFound(new { message = "Publisher not found" });
                }

                var publisherDto = new PublisherDto
                {
                    Id = publisher.Id,
                    Name = publisher.Name,
                    Description = publisher.Description,
                    TitleCount = publisher.Titles.Count,
                    CreatedDate = DateTime.UtcNow
                };

                return Ok(publisherDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving publisher {PublisherId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the publisher" });
            }
        }

        // POST: api/publisher
        [HttpPost]
        public async Task<ActionResult<PublisherDto>> CreatePublisher(CreatePublisherDto createPublisherDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                // Check if publisher with the same name already exists
                var existingPublisher = await _context.Publishers
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == createPublisherDto.Name.ToLower());

                if (existingPublisher != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A publisher with this name already exists",
                        errors = new[] { "Publisher name must be unique" }
                    });
                }

                var publisher = new Publisher
                {
                    Name = createPublisherDto.Name.Trim(),
                    Description = createPublisherDto.Description?.Trim() ?? string.Empty
                };

                _context.Publishers.Add(publisher);
                await _context.SaveChangesAsync();

                var publisherDto = new PublisherDto
                {
                    Id = publisher.Id,
                    Name = publisher.Name,
                    Description = publisher.Description,
                    TitleCount = 0,
                    CreatedDate = DateTime.UtcNow
                };

                _logger.LogInformation("Publisher created successfully: {PublisherName} by user {UserId}", publisher.Name, currentUser.Id);

                return CreatedAtAction(nameof(GetPublisher), new { id = publisher.Id }, new
                {
                    success = true,
                    message = "Publisher created successfully",
                    data = publisherDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating publisher");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the publisher",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // PUT: api/publisher/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePublisher(int id, UpdatePublisherDto updatePublisherDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input data",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var publisher = await _context.Publishers.FindAsync(id);
                if (publisher == null)
                {
                    return NotFound(new { success = false, message = "Publisher not found" });
                }

                // Check if another publisher with the same name already exists
                var existingPublisher = await _context.Publishers
                    .FirstOrDefaultAsync(p => p.Id != id && p.Name.ToLower() == updatePublisherDto.Name.ToLower());

                if (existingPublisher != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A publisher with this name already exists",
                        errors = new[] { "Publisher name must be unique" }
                    });
                }

                publisher.Name = updatePublisherDto.Name.Trim();
                publisher.Description = updatePublisherDto.Description?.Trim() ?? string.Empty;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Publisher updated successfully: {PublisherId} by user {UserId}", id, currentUser.Id);

                return Ok(new { success = true, message = "Publisher updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating publisher {PublisherId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the publisher",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // DELETE: api/publisher/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePublisher(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var publisher = await _context.Publishers
                    .Include(p => p.Titles)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (publisher == null)
                {
                    return NotFound(new { success = false, message = "Publisher not found" });
                }

                // Check if publisher has associated titles
                if (publisher.Titles.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete publisher with associated titles",
                        errors = new[] { $"Publisher has {publisher.Titles.Count} associated title(s)" }
                    });
                }

                _context.Publishers.Remove(publisher);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Publisher deleted successfully: {PublisherId} by user {UserId}", id, currentUser.Id);

                return Ok(new { success = true, message = "Publisher deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting publisher {PublisherId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the publisher",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // GET: api/publisher/search?query={query}
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PublisherListDto>>> SearchPublishers([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest(new { message = "Search query is required" });
                if (query.Length > 100)
                    return BadRequest(new { message = "Search query must not exceed 100 characters." });

                var publishers = await _context.Publishers
                    .Include(p => p.Titles)
                    .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
                    .Select(p => new PublisherListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        TitleCount = p.Titles.Count,
                        CreatedDate = DateTime.UtcNow
                    })
                    .OrderBy(p => p.Name)
                    .Take(50) // Limit results
                    .ToListAsync();

                return Ok(publishers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching publishers with query: {Query}", query);
                return StatusCode(500, new { message = "An error occurred while searching publishers" });
            }
        }

        // GET: api/publisher/health
        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "PublisherController"
            });
        }
    }
}