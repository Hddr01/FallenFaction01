using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Services
{
    public sealed class TitleChangeApplicator : ITitleChangeApplicator
    {
        private readonly ApplicationDbContext _db;

        public TitleChangeApplicator(ApplicationDbContext db) => _db = db;

        public IQueryable<TitleChangeLog> WithIncludesForApply(IQueryable<TitleChangeLog> source)
            => source
                .Include(tc => tc.Title).ThenInclude(t => t.Categories)
                .Include(tc => tc.Title).ThenInclude(t => t.Tags)
                .Include(tc => tc.Title).ThenInclude(t => t.Formats)
                .Include(tc => tc.Title).ThenInclude(t => t.Authors)
                .Include(tc => tc.Title).ThenInclude(t => t.Artists)
                .Include(tc => tc.Title).ThenInclude(t => t.Publishers)
                .Include(tc => tc.Title).ThenInclude(t => t.Teams);

        public async Task ApplyAsync(Title title, TitleChangeLog change, CancellationToken ct = default)
        {
            switch (change.ChangeType)
            {
                case TitleChangeTypes.OriginalTitle:
                    title.OriginalTitle = change.NewValue;
                    return;
                case TitleChangeTypes.EnglishTitle:
                    title.EnglishTitle = change.NewValue;
                    return;
                case TitleChangeTypes.Description:
                    title.Description = change.NewValue;
                    return;
                case TitleChangeTypes.AlternativeNames:
                    title.AlternativeNames = change.NewValue;
                    return;
                case TitleChangeTypes.ReleaseDate:
                    title.ReleaseDate = change.NewValue;
                    return;
                case TitleChangeTypes.Status:
                    title.StatusTitle = change.NewValue;
                    return;
                case TitleChangeTypes.TranslationStatus:
                    title.StatusTranslation = change.NewValue;
                    return;
                case TitleChangeTypes.Type:
                    if (Enum.TryParse<MangaType>(change.NewValue, out var mangaType))
                        title.Type = mangaType;
                    return;
                case TitleChangeTypes.AgeRestriction:
                    if (int.TryParse(change.NewValue, out var ageRestriction))
                        title.AgeRestriction = ageRestriction;
                    return;
                case TitleChangeTypes.CoverImage:
                    title.CoverImagePath = change.NewValue;
                    return;
                case TitleChangeTypes.BackgroundImage:
                    title.BackgroundImagePath = change.NewValue;
                    return;
                case TitleChangeTypes.Authors:
                    await ReplaceManyAsync<Author>(title.Authors, change.NewValue, ct);
                    return;
                case TitleChangeTypes.Artists:
                    await ReplaceManyAsync<Artist>(title.Artists, change.NewValue, ct);
                    return;
                case TitleChangeTypes.Publishers:
                    await ReplaceManyAsync<Publisher>(title.Publishers, change.NewValue, ct);
                    return;
                case TitleChangeTypes.Teams:
                    await ReplaceManyAsync<Team>(title.Teams, change.NewValue, ct);
                    return;
                case TitleChangeTypes.Categories:
                    await ReplaceManyAsync<Category>(title.Categories, change.NewValue, ct);
                    return;
                case TitleChangeTypes.Tags:
                    await ReplaceManyAsync<Tag>(title.Tags, change.NewValue, ct);
                    return;
                case TitleChangeTypes.Formats:
                    await ReplaceManyAsync<Format>(title.Formats, change.NewValue, ct);
                    return;
                case TitleChangeTypes.ExternalLinks:
                    title.ExternalLinksSerialized = change.NewValue;
                    return;
            }
        }

        private async Task ReplaceManyAsync<TEntity>(
            ICollection<TEntity> destination,
            string csv,
            CancellationToken ct)
            where TEntity : class
        {
            var ids = csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
            var entities = await _db.Set<TEntity>()
                .Where(e => ids.Contains(EF.Property<int>(e, "Id")))
                .ToListAsync(ct);
            destination.Clear();
            foreach (var entity in entities)
                destination.Add(entity);
        }
    }
}
