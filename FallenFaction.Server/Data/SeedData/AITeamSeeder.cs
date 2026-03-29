using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Data.SeedData
{
    /// <summary>
    /// Ensures the AI/TL system team exists on startup.
    /// Runs after roles/admin user are seeded in Program.cs.
    /// Creates the team owned by the first Admin user if it doesn't exist,
    /// then auto-unlocks the first 50 chapters of any AI Translation titles
    /// that haven't been processed yet.
    /// </summary>
    public static class AITeamSeeder
    {
        public const string AI_TEAM_NAME = "AI/TL";

        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<AppUser> userManager)
        {
            // Find the first admin user
            var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
            var admin = adminUsers.OrderBy(u => u.RegistrationDate).FirstOrDefault();

            if (admin == null)
            {
                Console.WriteLine("[AITeamSeeder] No admin user found — skipping.");
                return;
            }

            // Check if the AI/TL team already exists
            var existingTeam = await context.Teams
                .FirstOrDefaultAsync(t => t.IsSystemTeam && t.GroupType == GroupType.AITranslation);

            if (existingTeam != null)
            {
                Console.WriteLine($"[AITeamSeeder] AI/TL team already exists (Id={existingTeam.Id}).");
                await EnsureFirst50Unlocked(context);
                return;
            }

            // Create the AI/TL team
            var aiTeam = new Team
            {
                Name         = AI_TEAM_NAME,
                Description  = "Official AI Translation team. Novels in this group use Gemini AI translation with community ticket unlocks.",
                CreatorId    = admin.Id,
                GroupType    = GroupType.AITranslation,
                IsPersonal   = false,
                IsSystemTeam = true,
                CreatedDate  = DateTime.UtcNow
            };

            context.Teams.Add(aiTeam);
            await context.SaveChangesAsync();

            // Add admin as Admin-role member of the AI/TL team
            var adminRole = new UserTeamRole
            {
                AppUserId = admin.Id,
                TeamId    = aiTeam.Id,
                Role      = TeamRole.Admin
            };
            context.UserTeamRoles.Add(adminRole);
            await context.SaveChangesAsync();

            // Assign all permissions to the admin role
            await PermissionSeeder.AssignDefaultPermissionsToRole(context, adminRole);

            Console.WriteLine($"[AITeamSeeder] Created AI/TL system team (Id={aiTeam.Id}), owned by {admin.Email}.");

            await EnsureFirst50Unlocked(context);
        }

        /// <summary>
        /// For any AI Translation title that has never had its first-50 auto-unlock run,
        /// mark chapters 1–50 as unlocked (IsAILocked = false).
        /// This is idempotent — only touches chapters that are still locked.
        /// </summary>
        private static async Task EnsureFirst50Unlocked(ApplicationDbContext context)
        {
            // Find AI Translation titles
            var aiTitleIds = await context.Titles
                .Where(t => t.TitleCategory == TitleCategory.AITranslation)
                .Select(t => t.Id)
                .ToListAsync();

            if (!aiTitleIds.Any()) return;

            foreach (var titleId in aiTitleIds)
            {
                // Get the first 50 chapters ordered by chapter number
                var first50 = await context.Chapters
                    .Where(c => c.TitleId == titleId)
                    .OrderBy(c => c.VolumeNumber)
                    .ThenBy(c => c.ChapterNumber)
                    .Take(50)
                    .ToListAsync();

                bool changed = false;
                foreach (var chapter in first50)
                {
                    if (chapter.IsAILocked)
                    {
                        chapter.IsAILocked = false;
                        changed = true;
                    }
                }

                if (changed)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[AITeamSeeder] Auto-unlocked first-50 for title {titleId}.");
                }
            }
        }
    }
}
