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

        // ── FIX: Single SQL query computes all aggregates in the database.
        // Old code ran a GroupBy query and THEN pulled all comment rows for
        // client-side counting — loading potentially thousands of rows twice.
        IQueryable<Comment> query = _context.Comments.AsNoTracking();

        query = targetType == 1
            ? query.Where(c => c.TitleId == targetId && !c.IsDeleted)
            : query.Where(c => c.ChapterId == targetId && !c.IsDeleted);

        var stats = await query
            .GroupBy(c => 1)
            .Select(g => new CommentStatsDto
            {
                TotalComments = g.Count(),
                TopLevelComments = g.Count(c => c.ParentCommentId == null),
                Replies = g.Count(c => c.ParentCommentId != null),
                LastCommentDate = g.Max(c => (DateTime?)c.PostedDate),
                CommentsEnabled = commentsEnabled
            })
            .SingleOrDefaultAsync();

        return stats ?? new CommentStatsDto
        {
            TotalComments = 0,
            TopLevelComments = 0,
            Replies = 0,
            LastCommentDate = null,
            CommentsEnabled = commentsEnabled
        };
    }

    private async Task<bool> CheckCommentsEnabled(int targetId, int targetType)
    {
        // ── FIX: Added explicit OrderBy(id) to silence the EF Core
        // "FirstOrDefault without OrderBy" warning, and use a projection
        // (Select) so only the single boolean column is fetched — not the
        // whole entity row.
        return targetType switch
        {
            1 => await _context.Titles
                    .Where(t => t.Id == targetId)
                    .OrderBy(t => t.Id)
                    .Select(t => t.AreCommentsEnabled)
                    .FirstOrDefaultAsync(),

            2 => await _context.Chapters
                    .Where(c => c.Id == targetId)
                    .OrderBy(c => c.Id)
                    .Select(c => c.Title.AreChapterCommentsEnabled)
                    .FirstOrDefaultAsync(),

            _ => false
        };
    }
}