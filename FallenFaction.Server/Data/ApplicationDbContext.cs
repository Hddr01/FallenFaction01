using FallenFaction.Server.Data.Models;
using LibManga.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Title> Titles { get; set; }
    public DbSet<PendingTitle> PendingTitles { get; set; }
    public DbSet<RejectedTitle> RejectedTitles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Publisher> Publishers { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Format> Formats { get; set; }
    public DbSet<UserTeamRole> UserTeamRoles { get; set; }
    public DbSet<UserTeamPermission> UserTeamPermissions { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<CommentReaction> CommentReactions { get; set; } // Added CommentReactions DbSet
    public DbSet<PendingChapter> PendingChapters { get; set; }
    public DbSet<RejectedChapter> RejectedChapters { get; set; }
    public DbSet<ChapterImage> ChapterImages { get; set; }
    public DbSet<TitleChangeLog> TitleChangeLogs { get; set; }
    public DbSet<ApprovedTitleChange> ApprovedTitleChanges { get; set; }
    public DbSet<RejectedTitleChange> RejectedTitleChanges { get; set; }
    public DbSet<PendingTitleChange> PendingTitleChanges { get; set; }
    public DbSet<BookmarkFolder> BookmarkFolders { get; set; }
    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<ChapterView> ChapterViews { get; set; }

    public IQueryable<Chapter> GetUserChapters(string userId)
    {
        return Chapters.Where(c => c.UpdatedByUserId == userId);
    }

    public IQueryable<PendingChapter> GetUserPendingChapters(string userId)
    {
        return PendingChapters.Where(pc => pc.UpdatedByUserId == userId);
    }

    public IQueryable<RejectedChapter> GetUserRejectedChapters(string userId)
    {
        return RejectedChapters.Where(rc => rc.UpdatedByUserId == userId);
    }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Existing entity configurations
        builder.Entity<Title>()
            .HasMany(t => t.Categories)
            .WithMany(c => c.Titles)
            .UsingEntity(j => j.ToTable("TitleCategories"));

        builder.Entity<PendingTitle>()
            .HasMany(p => p.Categories)
            .WithMany(c => c.PendingTitles)
            .UsingEntity(j => j.ToTable("PendingTitleCategories"));

        builder.Entity<Title>()
            .HasMany(t => t.Tags)
            .WithMany(tg => tg.Titles)
            .UsingEntity(j => j.ToTable("TitleTags"));

        builder.Entity<PendingTitle>()
            .HasMany(p => p.Tags)
            .WithMany(tg => tg.PendingTitles)
            .UsingEntity(j => j.ToTable("PendingTitleTags"));

        builder.Entity<Title>()
            .HasMany(t => t.Formats)
            .WithMany(f => f.Titles)
            .UsingEntity(j => j.ToTable("TitleFormats"));

        builder.Entity<PendingTitle>()
            .HasMany(p => p.Formats)
            .WithMany(f => f.PendingTitles)
            .UsingEntity(j => j.ToTable("PendingTitleFormats"));

        builder.Entity<Title>()
            .HasMany(t => t.Authors)
            .WithMany(a => a.Titles)
            .UsingEntity(j => j.ToTable("TitleAuthors"));

        builder.Entity<PendingTitle>()
            .HasMany(p => p.Authors)
            .WithMany(a => a.PendingTitles)
            .UsingEntity(j => j.ToTable("PendingTitleAuthors"));

        builder.Entity<Title>()
            .HasMany(t => t.Artists)
            .WithMany(a => a.Titles)
            .UsingEntity(j => j.ToTable("TitleArtists"));

        builder.Entity<PendingTitle>()
            .HasMany(p => p.Artists)
            .WithMany(a => a.PendingTitles)
            .UsingEntity(j => j.ToTable("PendingTitleArtists"));

        builder.Entity<Title>()
            .HasMany(t => t.Publishers)
            .WithMany(p => p.Titles)
            .UsingEntity(j => j.ToTable("TitlePublishers"));

        builder.Entity<PendingTitle>()
            .HasMany(p => p.Publishers)
            .WithMany(p => p.PendingTitles)
            .UsingEntity(j => j.ToTable("PendingTitlePublishers"));

        builder.Entity<Title>()
            .HasMany(t => t.Teams)
            .WithMany(te => te.Titles)
            .UsingEntity(j => j.ToTable("TitleTeams"));

        builder.Entity<PendingTitle>()
            .HasMany(p => p.Teams)
            .WithMany(te => te.PendingTitles)
            .UsingEntity(j => j.ToTable("PendingTitleTeams"));

        // Configure many-to-many relationship between AppUser and Team
        builder.Entity<AppUser>()
            .HasMany(u => u.Teams)
            .WithMany(t => t.Members)
            .UsingEntity(j => j.ToTable("AppUserTeams"));

        builder.Entity<UserTeamRole>()
            .HasKey(utr => new { utr.AppUserId, utr.TeamId });

        builder.Entity<UserTeamRole>()
            .HasOne(utr => utr.AppUser)
            .WithMany(au => au.UserTeamRoles)
            .HasForeignKey(utr => utr.AppUserId);

        builder.Entity<UserTeamRole>()
            .HasOne(utr => utr.Team)
            .WithMany(t => t.UserTeamRoles)
            .HasForeignKey(utr => utr.TeamId);

        builder.Entity<UserTeamRole>()
            .HasMany(utr => utr.UserTeamRolePermissions)
            .WithOne(utrp => utrp.UserTeamRole)
            .HasForeignKey(utrp => new { utrp.AppUserId, utrp.TeamId });

        builder.Entity<UserTeamRolePermission>()
            .HasKey(utrp => new { utrp.AppUserId, utrp.TeamId, utrp.PermissionId });

        builder.Entity<UserTeamRolePermission>()
            .HasOne(utrp => utrp.UserTeamPermission)
            .WithMany(utp => utp.UserTeamRolePermissions)
            .HasForeignKey(utrp => utrp.PermissionId);

        // Configure Chapter, PendingChapter, and RejectedChapter relationships
        builder.Entity<Chapter>()
            .HasOne(c => c.Title)
            .WithMany(t => t.Chapters)
            .HasForeignKey(c => c.TitleId);

        builder.Entity<Chapter>()
            .HasOne(c => c.Team)
            .WithMany(t => t.Chapters)
            .HasForeignKey(c => c.TeamId);

        builder.Entity<PendingChapter>()
            .HasOne(pc => pc.Title)
            .WithMany(t => t.PendingChapters)
            .HasForeignKey(pc => pc.TitleId);

        builder.Entity<PendingChapter>()
            .HasOne(pc => pc.Team)
            .WithMany(t => t.PendingChapters)
            .HasForeignKey(pc => pc.TeamId);

        builder.Entity<RejectedChapter>()
            .HasOne(rc => rc.Title)
            .WithMany(t => t.RejectedChapters)
            .HasForeignKey(rc => rc.TitleId);

        builder.Entity<RejectedChapter>()
            .HasOne(rc => rc.Team)
            .WithMany(t => t.RejectedChapters)
            .HasForeignKey(rc => rc.TeamId);

        // Configuring ChapterImage to support optional foreign keys to Chapter types with no cascading deletes
        builder.Entity<ChapterImage>()
            .HasOne(ci => ci.Chapter)
            .WithMany(c => c.ImagePaths)
            .HasForeignKey(ci => ci.ChapterId)
            .OnDelete(DeleteBehavior.NoAction);  // Set NoAction to avoid multiple cascade paths

        builder.Entity<ChapterImage>()
            .HasOne(ci => ci.PendingChapter)
            .WithMany(pc => pc.ImagePaths)
            .HasForeignKey(ci => ci.PendingChapterId)
            .OnDelete(DeleteBehavior.NoAction);  // Set NoAction to avoid multiple cascade paths

        builder.Entity<ChapterImage>()
            .HasOne(ci => ci.RejectedChapter)
            .WithMany(rc => rc.ImagePaths)
            .HasForeignKey(ci => ci.RejectedChapterId)
            .OnDelete(DeleteBehavior.NoAction);  // Set NoAction to avoid multiple cascade paths

        // Configure the relationship between Title and TitleChangeLog
        builder.Entity<TitleChangeLog>()
            .HasOne(t => t.Title)
            .WithMany(t => t.ChangeLogs)
            .HasForeignKey(t => t.TitleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TitleChangeLog>()
            .HasOne(t => t.UpdatedByUser)
            .WithMany()
            .HasForeignKey(t => t.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);  // Changed to NoAction

        builder.Entity<TitleChangeLog>()
            .HasOne(t => t.ReviewedByUser)
            .WithMany()
            .HasForeignKey(t => t.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction);  // Changed to NoAction

        // Add this new configuration here
        builder.Entity<TitleChangeLog>()
            .Property(t => t.ReviewedByUserId)
            .IsRequired(false);
            
        // Make RejectionReason nullable to prevent similar errors
        builder.Entity<TitleChangeLog>()
            .Property(t => t.RejectionReason)
            .IsRequired(false);
        
        // Make AdminComment nullable in PendingTitleChanges
        builder.Entity<PendingTitleChange>()
            .Property(t => t.AdminComment)
            .IsRequired(false);


        // For PendingTitleChange
        builder.Entity<PendingTitleChange>()
            .HasOne(t => t.Title)
            .WithMany(t => t.PendingTitleChanges)
            .HasForeignKey(t => t.TitleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PendingTitleChange>()
            .HasOne(t => t.UpdatedByUser)
            .WithMany()
            .HasForeignKey(t => t.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // For ApprovedTitleChange
        builder.Entity<ApprovedTitleChange>()
            .HasOne(t => t.Title)
            .WithMany(t => t.ApprovedTitleChanges)
            .HasForeignKey(t => t.TitleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApprovedTitleChange>()
            .HasOne(t => t.UpdatedByUser)
            .WithMany()
            .HasForeignKey(t => t.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ApprovedTitleChange>()
            .HasOne(t => t.ReviewedByUser)
            .WithMany()
            .HasForeignKey(t => t.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // For RejectedTitleChange
        builder.Entity<RejectedTitleChange>()
            .HasOne(t => t.Title)
            .WithMany(t => t.RejectedTitleChanges)
            .HasForeignKey(t => t.TitleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RejectedTitleChange>()
            .HasOne(t => t.UpdatedByUser)
            .WithMany()
            .HasForeignKey(t => t.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<RejectedTitleChange>()
            .HasOne(t => t.ReviewedByUser)
            .WithMany()
            .HasForeignKey(t => t.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<BookmarkFolder>()
            .HasMany(f => f.Bookmarks)
            .WithOne(b => b.Folder)
            .HasForeignKey(b => b.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bookmark>(entity =>
        {
            entity.HasOne(b => b.Title)
                .WithMany(t => t.Bookmarks)
                .HasForeignKey(b => b.TitleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        builder.Entity<Bookmark>()
               .HasOne(b => b.User)
               .WithMany(u => u.Bookmarks)
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ChapterView>()
            .HasOne(cv => cv.Chapter)
            .WithMany(c => c.Views)
            .HasForeignKey(cv => cv.ChapterId)
            .OnDelete(DeleteBehavior.NoAction);  // Change from Cascade to NoAction

        builder.Entity<ChapterView>()
            .HasOne(cv => cv.User)
            .WithMany(u => u.ChapterViews)
            .HasForeignKey(cv => cv.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Comment and CommentReaction relationships
        builder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Comment>()
            .HasOne(c => c.Title)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TitleId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.Entity<Comment>()
            .HasOne(c => c.Chapter)
            .WithMany()
            .HasForeignKey(c => c.ChapterId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.Entity<Comment>()
            .HasOne(c => c.ChapterImage)
            .WithMany()
            .HasForeignKey(c => c.ChapterImageId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.Entity<CommentReaction>()
            .HasOne(r => r.User)
            .WithMany(u => u.CommentReactions)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CommentReaction>()
            .HasOne(r => r.Comment)
            .WithMany(c => c.Reactions)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.NoAction);

        // Add a unique constraint for user reactions on comments
        builder.Entity<CommentReaction>()
            .HasIndex(r => new { r.UserId, r.CommentId })
            .IsUnique();

        SeedData.Seed(builder);
    }
}
}