// Controllers/ArtistController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Artist;

namespace FallenFaction.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ArtistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ArtistController> _logger;

        public ArtistController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            ILogger<ArtistController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/artist
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ArtistListDto>>> GetArtists()
        {
            try
            {
                var artists = await _context.Artists
                    .Include(a => a.Titles)
                    .Select(a => new ArtistListDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        OtherName = a.OtherName,
                        Description = a.Description,
                        TitleCount = a.Titles.Count,
                        CreatedDate = DateTime.UtcNow // You might want to add CreatedDate to Artist model
                    })
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                return Ok(artists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving artists");
                return StatusCode(500, new { message = "An error occurred while retrieving artists" });
            }
        }

        // GET: api/artist/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ArtistDto>> GetArtist(int id)
        {
            try
            {
                var artist = await _context.Artists
                    .Include(a => a.Titles)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (artist == null)
                {
                    return NotFound(new { message = "Artist not found" });
                }

                var artistDto = new ArtistDto
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    OtherName = artist.OtherName,
                    Description = artist.Description,
                    TitleCount = artist.Titles.Count,
                    CreatedDate = DateTime.UtcNow
                };

                return Ok(artistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving artist {ArtistId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the artist" });
            }
        }

        // POST: api/artist
        [HttpPost]
        public async Task<ActionResult<ArtistDto>> CreateArtist(CreateArtistDto createArtistDto)
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

                // Check if artist with the same name already exists
                var existingArtist = await _context.Artists
                    .FirstOrDefaultAsync(a => a.Name.ToLower() == createArtistDto.Name.ToLower());

                if (existingArtist != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "An artist with this name already exists",
                        errors = new[] { "Artist name must be unique" }
                    });
                }

                var artist = new Artist
                {
                    Name = createArtistDto.Name.Trim(),
                    OtherName = createArtistDto.OtherName?.Trim() ?? string.Empty,
                    Description = createArtistDto.Description?.Trim() ?? string.Empty
                };

                _context.Artists.Add(artist);
                await _context.SaveChangesAsync();

                var artistDto = new ArtistDto
                {
                    Id = artist.Id,
                    Name = artist.Name,
                    OtherName = artist.OtherName,
                    Description = artist.Description,
                    TitleCount = 0,
                    CreatedDate = DateTime.UtcNow
                };

                _logger.LogInformation("Artist created successfully: {ArtistName} by user {UserId}", artist.Name, currentUser.Id);

                return CreatedAtAction(nameof(GetArtist), new { id = artist.Id }, new
                {
                    success = true,
                    message = "Artist created successfully",
                    data = artistDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating artist");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the artist",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // PUT: api/artist/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArtist(int id, UpdateArtistDto updateArtistDto)
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

                var artist = await _context.Artists.FindAsync(id);
                if (artist == null)
                {
                    return NotFound(new { success = false, message = "Artist not found" });
                }

                // Check if another artist with the same name already exists
                var existingArtist = await _context.Artists
                    .FirstOrDefaultAsync(a => a.Id != id && a.Name.ToLower() == updateArtistDto.Name.ToLower());

                if (existingArtist != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "An artist with this name already exists",
                        errors = new[] { "Artist name must be unique" }
                    });
                }

                artist.Name = updateArtistDto.Name.Trim();
                artist.OtherName = updateArtistDto.OtherName?.Trim() ?? string.Empty;
                artist.Description = updateArtistDto.Description?.Trim() ?? string.Empty;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Artist updated successfully: {ArtistId} by user {UserId}", id, currentUser.Id);

                return Ok(new { success = true, message = "Artist updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating artist {ArtistId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating the artist",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // DELETE: api/artist/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var artist = await _context.Artists
                    .Include(a => a.Titles)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (artist == null)
                {
                    return NotFound(new { success = false, message = "Artist not found" });
                }

                // Check if artist has associated titles
                if (artist.Titles.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete artist with associated titles",
                        errors = new[] { $"Artist has {artist.Titles.Count} associated title(s)" }
                    });
                }

                _context.Artists.Remove(artist);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Artist deleted successfully: {ArtistId} by user {UserId}", id, currentUser.Id);

                return Ok(new { success = true, message = "Artist deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting artist {ArtistId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the artist",
                    errors = new[] { "Internal server error" }
                });
            }
        }

        // GET: api/artist/search?query={query}
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ArtistListDto>>> SearchArtists([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "Search query is required" });
                }

                var artists = await _context.Artists
                    .Include(a => a.Titles)
                    .Where(a => a.Name.Contains(query) || a.OtherName.Contains(query))
                    .Select(a => new ArtistListDto
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

                return Ok(artists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching artists with query: {Query}", query);
                return StatusCode(500, new { message = "An error occurred while searching artists" });
            }
        }

        // GET: api/artist/health
        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "ArtistController"
            });
        }
    }
}