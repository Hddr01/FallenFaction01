using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Extensions;

public static class AppUserExtensions
{
    public static string GetDisplayName(this AppUser? user, string fallback = "Unknown")
        => user?.ProfileName ?? user?.UserName ?? fallback;
}
