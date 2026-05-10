using FallenFaction.Server.Data;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FallenFaction.Server.Services
{
    public sealed class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public PermissionService(ApplicationDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<bool> IsAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && await _userManager.IsInRoleAsync(user, "Admin");
        }

        public async Task<bool> CanEditTitleAsync(string userId, Title title)
        {
            if (await IsAdminAsync(userId)) return true;
            if (title.CreatedByUserId == userId) return true;

            var titleTeamIds = title.Teams.Select(t => t.Id).ToList();
            if (titleTeamIds.Count == 0) return false;

            var userTeamIds = await GetTeamIdsWithPermissionAsync(userId, Permissions.CanEditTitle);
            return userTeamIds.Intersect(titleTeamIds).Any();
        }

        public async Task<bool> CanViewAllTitleChangesAsync(string userId, int titleId)
        {
            if (await IsAdminAsync(userId)) return true;

            var title = await _db.Titles
                .Include(t => t.Teams)
                .FirstOrDefaultAsync(t => t.Id == titleId);

            if (title == null) return false;
            if (title.CreatedByUserId == userId) return true;

            var titleTeamIds = title.Teams.Select(t => t.Id).ToList();
            if (titleTeamIds.Count == 0) return false;

            var userTeamIds = await GetTeamIdsWithPermissionAsync(userId, Permissions.CanEditTitle);
            return userTeamIds.Intersect(titleTeamIds).Any();
        }

        public async Task<IReadOnlyList<int>> GetTeamIdsWithPermissionAsync(string userId, string permissionName)
        {
            return await _db.UserTeamRoles
                .Where(utr => utr.AppUserId == userId)
                .Where(utr =>
                    // Team creators (owners) implicitly have every permission.
                    utr.Team.CreatorId == userId ||
                    // Team admins implicitly have every permission.
                    utr.Role == TeamRole.Admin ||
                    // Members must hold the named permission explicitly.
                    (utr.Role == TeamRole.Member &&
                     utr.UserTeamRolePermissions.Any(p => p.UserTeamPermission.PermissionName == permissionName))
                )
                .Select(utr => utr.TeamId)
                .Distinct()
                .ToListAsync();
        }
    }
}
