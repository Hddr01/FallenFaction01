using FallenFaction.Server.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Data.SeedData
{
    public static class SeedData
    {
        public static void Seed(ModelBuilder builder)
        {
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Adventure" },
                new Category { Id = 2, Name = "Fantasy" },
                new Category { Id = 3, Name = "Horror" }
            );

            builder.Entity<Tag>().HasData(
                new Tag { Id = 1, Name = "Fantasy" },
                new Tag { Id = 2, Name = "Magic" },
                new Tag { Id = 3, Name = "Mystery" }
            );

            builder.Entity<Format>().HasData(
                new Format { Id = 1, Name = "Digital" },
                new Format { Id = 2, Name = "Print" }
            );

            builder.Entity<UserTeamPermission>().HasData(
                new UserTeamPermission { Id = 1, PermissionName = "CanAddTitle" },
                new UserTeamPermission { Id = 2, PermissionName = "CanDeleteTitle" },
                new UserTeamPermission { Id = 3, PermissionName = "CanEditTitle" },
                new UserTeamPermission { Id = 4, PermissionName = "CanAddMember" },
                new UserTeamPermission { Id = 5, PermissionName = "CanRemoveMember" },
                new UserTeamPermission { Id = 6, PermissionName = "CanAddChapter" },
                new UserTeamPermission { Id = 7, PermissionName = "CanEditChapter" },
                new UserTeamPermission { Id = 8, PermissionName = "CanDeleteChapter" },
                new UserTeamPermission { Id = 9, PermissionName = "CanViewAnalytics" }
            );
        }
    }
}