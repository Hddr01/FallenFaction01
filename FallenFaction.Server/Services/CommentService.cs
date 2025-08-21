// Updated CommentService.cs to handle soft deletes and use your Title model
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Comment;
using Microsoft.EntityFrameworkCore;

public interface ICommentService
{
    Task<CommentStatsDto> GetCommentStatsAsync(int targetId, int targetType);
}

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;

    public CommentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CommentStatsDto> GetCommentStatsAsync(int targetId, int targetType)
    {
        if (targetType < 1 || targetType > 3)
            throw new ArgumentException("Invalid target type");

        // Check if comments are enabled for the target
        bool commentsEnabled = await CheckCommentsEnabled(targetId, targetType);

        IQueryable<Comment> query = _context.Comments.AsQueryable();

        switch (targetType)
        {
            case 1: // Title
                query = query.Where(c => c.TitleId == targetId && !c.IsDeleted);
                break;
            case 2: // Chapter
                query = query.Where(c => c.ChapterId == targetId && !c.IsDeleted);
                break;
            case 3: // ChapterImage
                query = query.Where(c => c.ChapterImageId == targetId && !c.IsDeleted);
                break;
        }

        var stats = await query
            .GroupBy(c => 1)
            .Select(g => new
            {
                TotalComments = g.Count(),
                TopLevelComments = g.Count(c => c.ParentCommentId == null),
                LastCommentDate = g.Max(c => (DateTime?)c.PostedDate)
            })
            .FirstOrDefaultAsync();

        if (stats == null)
        {
            return new CommentStatsDto
            {
                TotalComments = 0,
                TopLevelComments = 0,
                Replies = 0,
                LastCommentDate = null,
                CommentsEnabled = commentsEnabled
            };
        }

        var comments = await query.ToListAsync();

        return new CommentStatsDto
        {
            TotalComments = comments.Count,
            TopLevelComments = comments.Count(c => c.ParentCommentId == null),
            Replies = comments.Count(c => c.ParentCommentId != null),
            LastCommentDate = comments.OrderByDescending(c => c.PostedDate).FirstOrDefault()?.PostedDate,
            CommentsEnabled = commentsEnabled
        };
    }

    private async Task<bool> CheckCommentsEnabled(int targetId, int targetType)
    {
        switch (targetType)
        {
            case 1: // Title comments
                var title = await _context.Titles.FirstOrDefaultAsync(t => t.Id == targetId);
                return title?.AreCommentsEnabled ?? true; // Default to enabled if title not found

            case 2: // Chapter comments
                var chapter = await _context.Chapters
                    .Include(c => c.Title)
                    .FirstOrDefaultAsync(c => c.Id == targetId);
                return chapter?.Title?.AreChapterCommentsEnabled ?? true; // Default to enabled

            case 3: // ChapterImage comments
                var chapterImage = await _context.ChapterImages
                    .Include(ci => ci.Chapter)
                    .ThenInclude(c => c.Title)
                    .FirstOrDefaultAsync(ci => ci.Id == targetId);
                return chapterImage?.Chapter?.Title?.AreChapterCommentsEnabled ?? true; // Default to enabled

            default:
                return false; // Invalid target type
        }
    }
}