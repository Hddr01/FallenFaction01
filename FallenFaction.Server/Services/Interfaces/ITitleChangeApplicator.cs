using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Services.Interfaces
{
    public interface ITitleChangeApplicator
    {
        // Eager-loads every Title navigation that ApplyAsync may mutate, so callers
        // don't have to remember the full Include tree.
        IQueryable<TitleChangeLog> WithIncludesForApply(IQueryable<TitleChangeLog> source);

        // Mutates `title` according to `change`. Caller owns SaveChanges and the
        // surrounding transaction. Unknown ChangeType values are ignored to match
        // pre-extraction behavior.
        Task ApplyAsync(Title title, TitleChangeLog change, CancellationToken ct = default);
    }
}
