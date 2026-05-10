namespace FallenFaction.Server.Services
{
    // Single source of truth for the named team permissions stored in
    // UserTeamPermission.PermissionName. New permissions added here so callers
    // (and the eventual Can(user, action) module) stop using string literals.
    public static class Permissions
    {
        public const string CanAddTitle = "CanAddTitle";
        public const string CanEditTitle = "CanEditTitle";
    }
}
