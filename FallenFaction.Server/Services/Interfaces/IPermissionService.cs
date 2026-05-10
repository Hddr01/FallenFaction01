using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Services.Interfaces
{
    // Boolean-question API for permission checks. Replaces inline LINQ walks of
    // UserTeamRoles + UserTeamRolePermissions that lived as private helpers across
    // controllers — Adding a new permission no longer means editing every endpoint
    // that should enforce it.
    public interface IPermissionService
    {
        // Site-admin role check. Wraps UserManager.IsInRoleAsync so callers
        // don't have to manage the user-loading dance.
        Task<bool> IsAdminAsync(string userId);

        // True if the user is a site admin, owns the title, or has CanEditTitle
        // in any of the title's teams.
        Task<bool> CanEditTitleAsync(string userId, Title title);

        // True if the user is a site admin, owns the title, or has CanEditTitle
        // in any team attached to the title. Used to gate the change-log surface
        // (full history vs approved-only).
        Task<bool> CanViewAllTitleChangesAsync(string userId, int titleId);

        // Returns team IDs where the user holds `permissionName` either by role
        // (creator / admin) or via UserTeamRolePermission. Used by AddTitle form
        // data to enumerate eligible teams.
        Task<IReadOnlyList<int>> GetTeamIdsWithPermissionAsync(string userId, string permissionName);
    }
}
