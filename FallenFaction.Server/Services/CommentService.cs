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
        if (targetType < 1 || targetType > 2)
            throw new ArgumentException("Invalid target type. Must be 1 (Title) or 2 (Chapter).");

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
            case 1:
                var title = await _context.Titles.FirstOrDefaultAsync(t => t.Id == targetId);
                return title?.AreCommentsEnabled ?? true;
            case 2:
                var chapter = await _context.Chapters
                    .Include(c => c.Title)
                    .FirstOrDefaultAsync(c => c.Id == targetId);
                return chapter?.Title?.AreChapterCommentsEnabled ?? true;
            default:
                return false;
        }
    }
}