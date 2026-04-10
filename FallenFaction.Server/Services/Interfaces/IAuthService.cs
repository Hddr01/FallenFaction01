using FallenFaction.Server.DTOs.Auth;

namespace FallenFaction.Server.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> AcceptTermsAndLoginAsync(AcceptTermsDto dto);
        Task<bool> LogoutAsync(string userId);
        Task<UserDto?> GetUserProfileAsync(string userId);
        Task<bool> UserExistsAsync(string email);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);

        // Email confirmation
        Task<AuthResponseDto> ConfirmEmailAsync(string userId, string token);
        Task<AuthResponseDto> ResendConfirmationEmailAsync(string email);

        // Online status management
        Task<bool> UpdateOnlineStatusAsync(string userId, bool isOnline);
        Task<bool> UpdateLastActiveAsync(string userId);
        Task SetUserOfflineAsync(string userId);
        Task<List<UserDto>> GetOnlineUsersAsync();
    }
}