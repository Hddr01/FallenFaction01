using FallenFaction.Server.Data.Models;
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
        // ADD THIS MISSING DbSet
        public DbSet<UserTeamRolePermission> UserTeamRolePermissions { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentReaction> CommentReactions { get; set; }
        public DbSet<PendingChapter> PendingChapters { get; set; }
        public DbSet<RejectedChapter> RejectedChapters { get; set; }

        public DbSet<TitleChangeLog> TitleChangeLogs { get; set; }
        public DbSet<ApprovedTitleChange> ApprovedTitleChanges { get; set; }
        public DbSet<RejectedTitleChange> RejectedTitleChanges { get; set; }
        public DbSet<PendingTitleChange> PendingTitleChanges { get; set; }
        public DbSet<BookmarkFolder> BookmarkFolders { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<ReadingProgress> ReadingProgress { get; set; }
        public DbSet<ChapterView> ChapterViews { get; set; }

        // ── Trust system ─────────────────────────────────────────────────────────
        public DbSet<UserTrustRecord> UserTrustRecords { get; set; }

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

            // Configure Title -> AppUser relationship with NO ACTION on delete
            builder.Entity<Title>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure PendingTitle -> AppUser relationship with NO ACTION on delete
            builder.Entity<PendingTitle>()
                .HasOne(pt => pt.CreatedByUser)
                .WithMany()
                .HasForeignKey(pt => pt.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure RejectedTitle -> AppUser relationship with NO ACTION on delete
            builder.Entity<RejectedTitle>()
                .HasOne(rt => rt.CreatedByUser)
                .WithMany()
                .HasForeignKey(rt => rt.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Title many-to-many relationships
            builder.Entity<Title>()
                .HasMany(t => t.Categories)
                .WithMany(c => c.Titles)
                .UsingEntity(j => j.ToTable("TitleCategories"));

            builder.Entity<Title>()
                .HasMany(t => t.Tags)
                .WithMany(tg => tg.Titles)
                .UsingEntity(j => j.ToTable("TitleTags"));

            builder.Entity<Title>()
                .HasMany(t => t.Formats)
                .WithMany(f => f.Titles)
                .UsingEntity(j => j.ToTable("TitleFormats"));

            builder.Entity<Title>()
                .HasMany(t => t.Authors)
                .WithMany(a => a.Titles)
                .UsingEntity(j => j.ToTable("TitleAuthors"));

            builder.Entity<Title>()
                .HasMany(t => t.Artists)
                .WithMany(a => a.Titles)
                .UsingEntity(j => j.ToTable("TitleArtists"));

            builder.Entity<Title>()
                .HasMany(t => t.Publishers)
                .WithMany(p => p.Titles)
                .UsingEntity(j => j.ToTable("TitlePublishers"));

            builder.Entity<Title>()
                .HasMany(t => t.Teams)
                .WithMany(te => te.Titles)
                .UsingEntity(j => j.ToTable("TitleTeams"));

            // PendingTitle many-to-many relationships
            builder.Entity<PendingTitle>()
                .HasMany(p => p.Categories)
                .WithMany(c => c.PendingTitles)
                .UsingEntity(j => j.ToTable("PendingTitleCategories"));

            builder.Entity<PendingTitle>()
                .HasMany(p => p.Tags)
                .WithMany(tg => tg.PendingTitles)
                .UsingEntity(j => j.ToTable("PendingTitleTags"));

            builder.Entity<PendingTitle>()
                .HasMany(p => p.Formats)
                .WithMany(f => f.PendingTitles)
                .UsingEntity(j => j.ToTable("PendingTitleFormats"));

            builder.Entity<PendingTitle>()
                .HasMany(p => p.Authors)
                .WithMany(a => a.PendingTitles)
                .UsingEntity(j => j.ToTable("PendingTitleAuthors"));

            builder.Entity<PendingTitle>()
                .HasMany(p => p.Artists)
                .WithMany(a => a.PendingTitles)
                .UsingEntity(j => j.ToTable("PendingTitleArtists"));

            builder.Entity<PendingTitle>()
                .HasMany(p => p.Publishers)
                .WithMany(p => p.PendingTitles)
                .UsingEntity(j => j.ToTable("PendingTitlePublishers"));

            builder.Entity<PendingTitle>()
                .HasMany(p => p.Teams)
                .WithMany(te => te.PendingTitles)
                .UsingEntity(j => j.ToTable("PendingTitleTeams"));

            // NOTE: RejectedTitle many-to-many relationships REMOVED to avoid compilation errors
            // RejectedTitle will store relationship data as collections without database relationships

            // Configure many-to-many relationship between AppUser and Team
            builder.Entity<AppUser>()
                .HasMany(u => u.Teams)
                .WithMany(t => t.Members)
                .UsingEntity(j => j.ToTable("AppUserTeams"));

            // UserTeamRole configuration
            builder.Entity<UserTeamRole>()
                .HasKey(utr => new { utr.AppUserId, utr.TeamId });

            builder.Entity<UserTeamRole>()
                .HasOne(utr => utr.AppUser)
                .WithMany(au => au.UserTeamRoles)
                .HasForeignKey(utr => utr.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserTeamRole>()
                .HasOne(utr => utr.Team)
                .WithMany(t => t.UserTeamRoles)
                .HasForeignKey(utr => utr.TeamId)
                .OnDelete(DeleteBehavior.NoAction);

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

            // Configure Chapter relationships
            builder.Entity<Chapter>()
                .HasOne(c => c.Title)
                .WithMany(t => t.Chapters)
                .HasForeignKey(c => c.TitleId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Chapter>()
                .HasOne(c => c.Team)
                .WithMany(t => t.Chapters)
                .HasForeignKey(c => c.TeamId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Chapter>()
                .HasOne(c => c.UpdatedByUser)
                .WithMany()
                .HasForeignKey(c => c.UpdatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PendingChapter>()
                .HasOne(pc => pc.Title)
                .WithMany(t => t.PendingChapters)
                .HasForeignKey(pc => pc.TitleId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PendingChapter>()
                .HasOne(pc => pc.Team)
                .WithMany(t => t.PendingChapters)
                .HasForeignKey(pc => pc.TeamId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<RejectedChapter>()
                .HasOne(rc => rc.Title)
                .WithMany(t => t.RejectedChapters)
                .HasForeignKey(rc => rc.TitleId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<RejectedChapter>()
                .HasOne(rc => rc.Team)
                .WithMany(t => t.RejectedChapters)
                .HasForeignKey(rc => rc.TeamId)
                .OnDelete(DeleteBehavior.NoAction);


            // TitleChangeLog relationships
            builder.Entity<TitleChangeLog>()
                .HasOne(t => t.Title)
                .WithMany(t => t.ChangeLogs)
                .HasForeignKey(t => t.TitleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TitleChangeLog>()
                .HasOne(t => t.UpdatedByUser)
                .WithMany()
                .HasForeignKey(t => t.UpdatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TitleChangeLog>()
                .HasOne(t => t.ReviewedByUser)
                .WithMany()
                .HasForeignKey(t => t.ReviewedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TitleChangeLog>()
                .Property(t => t.ReviewedByUserId)
                .IsRequired(false);

            builder.Entity<TitleChangeLog>()
                .Property(t => t.RejectionReason)
                .IsRequired(false);

            // PendingTitleChange relationships
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

            builder.Entity<PendingTitleChange>()
                .Property(t => t.AdminComment)
                .IsRequired(false);

            // ApprovedTitleChange relationships
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

            // RejectedTitleChange relationships
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

            // Rating configuration
            builder.Entity<Rating>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasAnnotation("Range", new[] { 1, 10 });

                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.TitleId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Ratings)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(r => r.Title)
                    .WithMany(t => t.Ratings)
                    .HasForeignKey(r => r.TitleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.UserId, r.TitleId })
                    .IsUnique()
                    .HasDatabaseName("IX_Ratings_UserId_TitleId");

                entity.HasIndex(r => r.TitleId)
                    .HasDatabaseName("IX_Ratings_TitleId");

                entity.HasIndex(r => r.CreatedAt)
                    .HasDatabaseName("IX_Ratings_CreatedAt");
            });

            // BookmarkFolder configuration
            builder.Entity<BookmarkFolder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            });

            builder.Entity<BookmarkFolder>()
                .HasMany(f => f.Bookmarks)
                .WithOne(b => b.Folder)
                .HasForeignKey(b => b.FolderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bookmark configuration
            builder.Entity<Bookmark>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.HasIndex(e => new { e.UserId, e.TitleId }).IsUnique();
            });

            builder.Entity<Bookmark>()
               .HasOne(b => b.User)
               .WithMany(u => u.Bookmarks)
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Bookmark>()
               .HasOne(b => b.Title)
               .WithMany(t => t.Bookmarks)
               .HasForeignKey(b => b.TitleId)
               .OnDelete(DeleteBehavior.Cascade);

            // ChapterView configuration
            builder.Entity<ChapterView>()
                .HasOne(cv => cv.Chapter)
                .WithMany(c => c.Views)
                .HasForeignKey(cv => cv.ChapterId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ChapterView>()
                .HasOne(cv => cv.User)
                .WithMany(u => u.ChapterViews)
                .HasForeignKey(cv => cv.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment configuration
            builder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(c => c.DeletedByUser)
                      .WithMany()
                      .HasForeignKey(c => c.DeletedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(c => c.Title)
                      .WithMany(t => t.Comments)
                      .HasForeignKey(c => c.TitleId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired(false);

                entity.HasOne(c => c.Chapter)
                      .WithMany()
                      .HasForeignKey(c => c.ChapterId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired(false);


                entity.HasOne(c => c.ParentComment)
                      .WithMany(c => c.Replies)
                      .HasForeignKey(c => c.ParentCommentId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                entity.Property(c => c.PostedDate)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(c => c.Content)
                      .IsRequired()
                      .HasMaxLength(2000);

                entity.Property(c => c.LikesCount)
                      .HasDefaultValue(0);

                entity.Property(c => c.DislikesCount)
                      .HasDefaultValue(0);

                entity.Property(c => c.IsDeleted)
                      .HasDefaultValue(false);
            });

            // CommentReaction configuration
            builder.Entity<CommentReaction>(entity =>
            {
                entity.HasKey(cr => cr.Id);

                entity.HasIndex(cr => new { cr.CommentId, cr.UserId })
                      .IsUnique()
                      .HasDatabaseName("IX_CommentReactions_CommentId_UserId");

                entity.HasOne(cr => cr.Comment)
                      .WithMany(c => c.Reactions)
                      .HasForeignKey(cr => cr.CommentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cr => cr.User)
                      .WithMany(u => u.CommentReactions)
                      .HasForeignKey(cr => cr.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(cr => cr.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
            });


            // ReadingProgress configuration
            builder.Entity<ReadingProgress>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Create composite unique index for fast lookups by user and title
                entity.HasIndex(rp => new { rp.UserId, rp.TitleId })
                      .IsUnique()
                      .HasDatabaseName("IX_ReadingProgress_UserId_TitleId");

                // Foreign key to Title
                entity.HasOne(rp => rp.Title)
                      .WithMany()
                      .HasForeignKey(rp => rp.TitleId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Foreign key to User
                entity.HasOne(rp => rp.User)
                      .WithMany()
                      .HasForeignKey(rp => rp.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Required properties
                entity.Property(rp => rp.UserId).IsRequired();
                entity.Property(rp => rp.TitleId).IsRequired();
                entity.Property(rp => rp.LastReadChapter).IsRequired();
                entity.Property(rp => rp.LastReadDate).IsRequired();
            });
            // UserTrustRecord configuration
            builder.Entity<UserTrustRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.ActionType }).IsUnique();
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.UserId).IsRequired();
            });

            // Call the seed data from LibManga.Data namespace
            FallenFaction.Server.Data.SeedData.SeedData.Seed(builder);
        }
    }
}