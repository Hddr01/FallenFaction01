// Create this as Data/SeedData/PermissionSeeder.cs - FIXED VERSION
using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Data.SeedData
{
    public static class PermissionSeeder
    {
        public static async Task SeedPermissions(ApplicationDbContext context)
        {
            // Define the permissions that should exist - using consistent naming
            var permissions = new[]
            {
                "CanAddTitle",      // Matches existing SeedData.cs
                "CanEditTitle",     // Matches existing SeedData.cs  
                "CanDeleteTitle",   // Matches existing SeedData.cs
                "CanAddMember",     // Matches existing SeedData.cs
                "CanRemoveMember",  // Matches existing SeedData.cs
                "CanAddChapter",    // New permission for chapters
                "CanEditChapter",   // New permission for chapters
                "CanDeleteChapter", // New permission for chapters
                "CanViewAnalytics"  // New permission for analytics
            };

            // Add permissions if they don't exist
            foreach (var permissionName in permissions)
            {
                var existingPermission = await context.UserTeamPermissions
                    .FirstOrDefaultAsync(p => p.PermissionName == permissionName);

                if (existingPermission == null)
                {
                    var permission = new UserTeamPermission
                    {
                        PermissionName = permissionName
                    };

                    context.UserTeamPermissions.Add(permission);
                }
            }

            await context.SaveChangesAsync();

            // Now assign default permissions to existing team roles
            await AssignDefaultPermissions(context);
        }

        private static async Task AssignDefaultPermissions(ApplicationDbContext context)
        {
            var permissions = await context.UserTeamPermissions.ToListAsync();
            var permissionLookup = permissions.ToDictionary(p => p.PermissionName, p => p.Id);

            // Get all user team roles that don't have permissions assigned yet
            var userTeamRoles = await context.UserTeamRoles
                .Include(utr => utr.UserTeamRolePermissions)
                .ToListAsync();

            foreach (var userTeamRole in userTeamRoles)
            {
                // Skip if this user team role already has permissions
                if (userTeamRole.UserTeamRolePermissions.Any())
                    continue;

                List<string> permissionsToAssign = new();

                switch (userTeamRole.Role)
                {
                    case TeamRole.Admin:
                        // Admins get all permissions
                        permissionsToAssign = permissions.Select(p => p.PermissionName).ToList();
                        break;

                    case TeamRole.Member:
                        // Members get basic permissions - using the correct permission names
                        permissionsToAssign = new List<string>
                        {
                            "CanAddTitle",
                            "CanEditTitle",
                            "CanAddChapter",
                            "CanEditChapter"
                        };
                        break;

                    case TeamRole.Viewer:
                        // Viewers get no special permissions (just viewing)
                        permissionsToAssign = new List<string>();
                        break;
                }

                // Assign the permissions
                foreach (var permissionName in permissionsToAssign)
                {
                    if (permissionLookup.TryGetValue(permissionName, out var permissionId))
                    {
                        var rolePermission = new UserTeamRolePermission
                        {
                            AppUserId = userTeamRole.AppUserId,
                            TeamId = userTeamRole.TeamId,
                            PermissionId = permissionId
                        };

                        context.UserTeamRolePermissions.Add(rolePermission);
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        // Method to assign permissions when a new user team role is created
        public static async Task AssignDefaultPermissionsToRole(
            ApplicationDbContext context,
            UserTeamRole userTeamRole)
        {
            var permissions = await context.UserTeamPermissions.ToListAsync();
            var permissionLookup = permissions.ToDictionary(p => p.PermissionName, p => p.Id);

            List<string> permissionsToAssign = new();

            switch (userTeamRole.Role)
            {
                case TeamRole.Admin:
                    permissionsToAssign = permissions.Select(p => p.PermissionName).ToList();
                    break;

                case TeamRole.Member:
                    // Using the correct permission names that match your existing data
                    permissionsToAssign = new List<string>
                    {
                        "CanAddTitle",
                        "CanEditTitle",
                        "CanAddChapter",
                        "CanEditChapter"
                    };
                    break;

                case TeamRole.Viewer:
                    permissionsToAssign = new List<string>();
                    break;
            }

            foreach (var permissionName in permissionsToAssign)
            {
                if (permissionLookup.TryGetValue(permissionName, out var permissionId))
                {
                    var rolePermission = new UserTeamRolePermission
                    {
                        AppUserId = userTeamRole.AppUserId,
                        TeamId = userTeamRole.TeamId,
                        PermissionId = permissionId
                    };

                    context.UserTeamRolePermissions.Add(rolePermission);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}