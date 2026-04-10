// Controllers/AuthorController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Author;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AuthorController> _logger;

        public AuthorController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<AuthorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/author
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AuthorListDto>>> GetAuthors()
        {
            try
            {
                var authors = await _context.Authors
                    .Include(a => a.Titles)
                    .Select(a => new AuthorListDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        OtherName = a.OtherName,
                        Description = a.Description,
                        TitleCount = a.Titles.Count,
                        CreatedDate = DateTime.UtcNow // You might want to add CreatedDate to Author model
                    })
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                return Ok(authors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authors");
                return StatusCode(500, new { message = "An error occurred while retrieving authors" });
            }
        }

        // GET: api/author/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthorDto>> GetAuthor(int id)
        {
            try
            {
                var author = await _context.Authors
                    .Include(a => a.Titles)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (author == null)
                {
                    return NotFound(new { message = "Author not found" });
                }

                var authorDto = new AuthorDto
                {
                    Id = author.Id,
                    Name = author.Name,
                    OtherName = author.OtherName,
                    Description = author.Description,
                    TitleCount = author.Titles.Count,
                    CreatedDate = DateTime.UtcNow
                };

                return Ok(authorDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving author {AuthorId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the author" });
            }
        }

        // POST: api/author
        [HttpPost]
        public async Task<ActionResult<AuthorDto>> CreateAuthor(CreateAuthorDto createAuthorDto)
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

                // Check if author with the same name already exists
                var existingAuthor = await _context.Authors
                    .FirstOrDefaultAsync(a => a.Name.ToLower() == createAuthorDto.Name.ToLower());

                if (existingAuthor != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "An author with this name already exists",
                        errors = new[] { "Author name must be unique" }
                    });
                }

                var author = new Author
                {
                    Name = createAuthorDto.Name.Trim(),
                    OtherName = createAuthorDto.OtherName?.Trim() ?? string.Empty,
                    Description = createAuthorDto.Description?.Trim() ?? string.Empty
                };

                _context.Authors.Add(author);
                await _context.SaveChangesAsync();

                var authorDto = new AuthorDto
                {
                    Id = author.Id,
                    Name = author.Name,
                    OtherName = author.OtherName,
                    Description = author.Description,
                    TitleCount = 0,
                    CreatedDate = DateTime.UtcNow
                };

                _logger.LogInformation("Author created successfully: {AuthorName} by user {UserId}", author.Name, currentUser.Id);

                return CreatedAtAction(nameof(GetAuthor), new { id = author.Id }, new
                {
                    success = true,
                    message = "Author created successfully",
                    data = authorDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating author");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the author",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // PUT: api/author/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuthor(int id, UpdateAuthorDto updateAuthorDto)
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

                var author = await _context.Authors.FindAsync(id);
                if (author == null)
                {
                    return NotFound(new { success = false, message = "Author not found" });
                }

                // Check if another author with the same name already exists
                var existingAuthor = await _context.Authors
                    .FirstOrDefaultAsync(a => a.Id != id && a.Name.ToLower() == updateAuthorDto.Name.ToLower());

                if (existingAuthor != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "An author with this name already exists",
                        errors = new[] { "Author name must be unique" }
                    });
                }

                author.Name = updateAuthorDto.Name.Trim();
                author.OtherName = updateAuthorDto.OtherName?.Trim() ?? string.Empty;
                author.Description = updateAuthorDto.Description?.Trim() ?? string.Empty;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Author updated successfully: {AuthorId} by user {UserId}", id, currentUser.Id);

                return Ok(new { success = true, message = "Author updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating author {AuthorId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the author",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // DELETE: api/author/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var author = await _context.Authors
                    .Include(a => a.Titles)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (author == null)
                {
                    return NotFound(new { success = false, message = "Author not found" });
                }

                // Check if author has associated titles
                if (author.Titles.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete author with associated titles",
                        errors = new[] { $"Author has {author.Titles.Count} associated title(s)" }
                    });
                }

                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Author deleted successfully: {AuthorId} by user {UserId}", id, currentUser.Id);

                return Ok(new { success = true, message = "Author deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting author {AuthorId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the author",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // GET: api/author/search?query={query}
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AuthorListDto>>> SearchAuthors([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest(new { message = "Search query is required" });
                if (query.Length > 100)
                    return BadRequest(new { message = "Search query must not exceed 100 characters." });

                var authors = await _context.Authors
                    .Include(a => a.Titles)
                    .Where(a => a.Name.Contains(query) || a.OtherName.Contains(query))
                    .Select(a => new AuthorListDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        OtherName = a.OtherName,
                        Description = a.Description,
                        TitleCount = a.Titles.Count,
                        CreatedDate = DateTime.UtcNow
                    })
                    .OrderBy(a => a.Name)
                    .Take(50) // Limit results
                    .ToListAsync();

                return Ok(authors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching authors with query: {Query}", query);
                return StatusCode(500, new { message = "An error occurred while searching authors" });
            }
        }

        // GET: api/author/health
        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "AuthorController"
            });
        }
    }
}