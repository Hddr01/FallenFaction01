using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(AppUser user, IList<string> roles);
        Task<string> GenerateRefreshTokenAsync();
        Task<bool> ValidateRefreshTokenAsync(string refreshToken, string userId);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}