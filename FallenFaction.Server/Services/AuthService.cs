// Services/AuthService.cs - UPDATED with HTTPS profile picture migration
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using FallenFaction.Server.Constants;
using FallenFaction.Server.Data.Models;
using FallenFaction.Server.DTOs.Auth;
using FallenFaction.Server.Services.Interfaces;
using FallenFaction.Server.Data;

namespace FallenFaction.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        // Concurrent operation tracking
        private readonly Dictionary<string, SemaphoreSlim> _userUpdateSemaphores = new();
        private readonly SemaphoreSlim _semaphoreManagerLock = new(1, 1);

        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenService tokenService,
            IMapper mapper,
            ILogger<AuthService> logger,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        private string GetStaticAssetBaseUrl()
        {
            // No longer used to build returned URLs.
            // Kept only for any legacy code that still calls it.
            return string.Empty;
        }

        private string FixProfilePictureUrl(string? currentUrl)
        {
            if (string.IsNullOrEmpty(currentUrl))
                return "/img/default-avatar.png";   // relative  works from any origin

            // Already a clean relative path  return as-is
            if (currentUrl.StartsWith("/"))
                return currentUrl;

            // Strip any absolute prefix (http://localhost:xxxx or https://localhost:xxxx)
            // and return just the path portion so the frontend/nginx can serve it.
            try
            {
                var uri = new Uri(currentUrl);
                return uri.PathAndQuery;   // e.g. "/img/default-avatar.png" or "/uploads/avatars/..."
            }
            catch
            {
                // Not a valid URI  return the raw value and let the frontend handle it
                return currentUrl;
            }
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Check if user already exists
                if (await UserExistsAsync(registerDto.Email))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "A user with this email already exists.",
                        Errors = new List<string> { "Email is already registered." }
                    };
                }

                // Check if username already exists
                var existingUser = await _userManager.FindByNameAsync(registerDto.UserName);
                if (existingUser != null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "This username is already taken.",
                        Errors = new List<string> { "Username is already in use." }
                    };
                }

                if (!registerDto.AcceptedTerms)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "You must accept the Terms and Conditions.",
                        Errors = new List<string> { "Terms not accepted." }
                    };
                }

                // Create new user with HTTPS profile picture
                var user = new AppUser
                {
                    UserName = registerDto.UserName,
                    Email = registerDto.Email,
                    DateOfBirth = registerDto.DateOfBirth,
                    Bio = registerDto.Bio,
                    ProfilePicturePath = $"{GetStaticAssetBaseUrl()}/img/default-avatar.png",
                    RegistrationDate = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow,
                    IsActive = true,
                    IsOnline = true,
                    IsVerified = false,
                    IsBannedFromComments = false,
                    AcceptedTermsAt = DateTime.UtcNow,
                    AcceptedTermsVersion = TermsConstants.CurrentVersion
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Failed to create user account.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                // Assign default role
                await _userManager.AddToRoleAsync(user, "User");

                // Auto-create Personal Group on registration
                try
                {
                    var personalGroup = new Team
                    {
                        Name = user.UserName + "'s Studio",
                        Description = "Personal studio for " + user.UserName,
                        CreatorId = user.Id,
                        GroupType = GroupType.Personal,
                        IsPersonal = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Teams.Add(personalGroup);
                    await _context.SaveChangesAsync();

                    _context.UserTeamRoles.Add(new UserTeamRole
                    {
                        AppUserId = user.Id,
                        TeamId = personalGroup.Id,
                        Role = TeamRole.Admin
                    });
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Personal group created for {UserName}", user.UserName);
                }
                catch (Exception groupEx)
                {
                    _logger.LogError(groupEx, "Failed to create personal group for {UserId}", user.Id);
                }

                // Generate JWT token
                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.GenerateJwtToken(user, roles);

                // Map user to DTO
                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = roles.ToList();

                _logger.LogInformation("User {Email} registered successfully with username {UserName}", registerDto.Email, registerDto.UserName);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Registration successful.",
                    Token = token,
                    TokenExpiration = DateTime.UtcNow.AddHours(24),
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration for {Email}", registerDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during registration.",
                    Errors = new List<string> { "Internal server error occurred." }
                };
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password.",
                        Errors = new List<string> { "Authentication failed." }
                    };
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Account is deactivated. Please contact support.",
                        Errors = new List<string> { "Account deactivated." }
                    };
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

                if (!result.Succeeded)
                {
                    var errorMessage = result.IsLockedOut
                        ? "Account is locked out. Try again later."
                        : "Invalid email or password.";

                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = errorMessage,
                        Errors = new List<string> { "Authentication failed." }
                    };
                }

                if (user.AcceptedTermsAt == null)
                {
                    return new AuthResponseDto
                    {
                        Success = true,
                        Message = "Terms acceptance required before login.",
                        RequiresTermsAcceptance = true,
                        TermsVersion = TermsConstants.CurrentVersion,
                        Token = null,
                        User = null
                    };
                }

                // Update login info with improved retry logic and profile picture fix
                await UpdateUserWithSemaphoreAsync(user.Id, u =>
                {
                    u.LastLoginDate = DateTime.UtcNow;
                    u.LastActive = DateTime.UtcNow;
                    u.IsOnline = true;

                    // Fix profile picture URL during login
                    u.ProfilePicturePath = FixProfilePictureUrl(u.ProfilePicturePath);
                });

                // Generate JWT token
                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.GenerateJwtToken(user, roles);

                // Map user to DTO with fixed profile picture URL
                var userDto = _mapper.Map<UserDto>(user);
                userDto.ProfilePicturePath = FixProfilePictureUrl(userDto.ProfilePicturePath);
                userDto.Roles = roles.ToList();

                _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = token,
                    TokenExpiration = DateTime.UtcNow.AddHours(24),
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user login for {Email}", loginDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login.",
                    Errors = new List<string> { "Internal server error occurred." }
                };
            }
        }

        public async Task<AuthResponseDto> AcceptTermsAndLoginAsync(AcceptTermsDto dto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password.",
                        Errors = new List<string> { "Authentication failed." }
                    };
                }

                if (!user.IsActive)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Account is deactivated. Please contact support.",
                        Errors = new List<string> { "Account deactivated." }
                    };
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    var errorMessage = result.IsLockedOut
                        ? "Account is locked out. Try again later."
                        : "Invalid email or password.";

                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = errorMessage,
                        Errors = new List<string> { "Authentication failed." }
                    };
                }

                await UpdateUserWithSemaphoreAsync(user.Id, u =>
                {
                    u.AcceptedTermsAt = DateTime.UtcNow;
                    u.AcceptedTermsVersion = TermsConstants.CurrentVersion;
                    u.LastLoginDate = DateTime.UtcNow;
                    u.LastActive = DateTime.UtcNow;
                    u.IsOnline = true;
                    u.ProfilePicturePath = FixProfilePictureUrl(u.ProfilePicturePath);
                });

                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.GenerateJwtToken(user, roles);

                var fresh = await _userManager.FindByIdAsync(user.Id);
                if (fresh == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "An error occurred.",
                        Errors = new List<string> { "User not found after update." }
                    };
                }

                var userDto = _mapper.Map<UserDto>(fresh);
                userDto.ProfilePicturePath = FixProfilePictureUrl(userDto.ProfilePicturePath);
                userDto.Roles = roles.ToList();

                _logger.LogInformation("User {Email} accepted terms and logged in", dto.Email);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = token,
                    TokenExpiration = DateTime.UtcNow.AddHours(24),
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during accept terms for {Email}", dto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred.",
                    Errors = new List<string> { "Internal server error occurred." }
                };
            }
        }

        // Keep all other methods unchanged but add profile picture fixing to GetUserProfileAsync
        public async Task<UserDto?> GetUserProfileAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);
                var userDto = _mapper.Map<UserDto>(user);
                userDto.ProfilePicturePath = FixProfilePictureUrl(userDto.ProfilePicturePath);
                userDto.Roles = roles.ToList();

                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user profile for {UserId}", userId);
                return null;
            }
        }

        // Rest of the methods remain the same...
        public async Task<bool> LogoutAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("LogoutAsync called with null or empty userId");
                return true;
            }

            _logger.LogInformation("Processing logout for user {UserId}", userId);

            try
            {
                // Use a dedicated fast path for logout to avoid conflicts with other operations
                var success = await SetUserOfflineFastAsync(userId);

                if (success)
                {
                    _logger.LogInformation("User {UserId} logged out successfully", userId);
                }
                else
                {
                    _logger.LogWarning("Failed to update offline status for user {UserId} during logout, but allowing logout to proceed", userId);
                }

                // Always return true for logout - the client should clear its state regardless
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user logout for {UserId}", userId);
                // Return true to allow client-side logout to proceed
                return true;
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if user exists for {Email}", email);
                return false;
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Refresh token functionality not fully implemented.",
                    Errors = new List<string> { "Feature not available." }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token refresh");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during token refresh.",
                    Errors = new List<string> { "Internal server error occurred." }
                };
            }
        }

        public async Task<bool> UpdateOnlineStatusAsync(string userId, bool isOnline)
        {
            try
            {
                // Direct SQL UPDATE bypasses the ConcurrencyStamp check that
                // UserManager.UpdateAsync() performs, eliminating the race condition
                // when multiple browser tabs call this endpoint simultaneously.
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE AspNetUsers SET IsOnline = {0}, LastActive = {1} WHERE Id = {2}",
                    isOnline,
                    DateTime.UtcNow,
                    userId);

                if (rows == 0)
                    _logger.LogWarning("UpdateOnlineStatus: user {UserId} not found", userId);

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating online status for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateLastActiveAsync(string userId)
        {
            try
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE AspNetUsers SET IsOnline = 1, LastActive = {0} WHERE Id = {1}",
                    DateTime.UtcNow,
                    userId);

                if (rows == 0)
                    _logger.LogWarning("UpdateLastActive: user {UserId} not found", userId);

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last active for user {UserId}", userId);
                return false;
            }
        }

        public async Task SetUserOfflineAsync(string userId)
        {
            try
            {
                await SetUserOfflineFastAsync(userId);
                _logger.LogInformation("Set user {UserId} offline", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user {UserId} offline", userId);
            }
        }

        public async Task<List<UserDto>> GetOnlineUsersAsync()
        {
            try
            {
                // Get users who are marked as online and have been active in the last 5 minutes
                var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
                var onlineUsers = _userManager.Users
                    .Where(u => u.IsOnline && u.LastActive > cutoffTime)
                    .ToList();

                var userDtos = new List<UserDto>();
                foreach (var user in onlineUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var userDto = _mapper.Map<UserDto>(user);
                    userDto.ProfilePicturePath = FixProfilePictureUrl(userDto.ProfilePicturePath);
                    userDto.Roles = roles.ToList();
                    userDtos.Add(userDto);
                }

                return userDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users");
                return new List<UserDto>();
            }
        }



        // NEW: Fast offline update specifically for logout scenarios
        private async Task<bool> SetUserOfflineFastAsync(string userId)
        {
            try
            {
                // Use raw SQL for faster, more reliable logout updates
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found during logout", userId);
                    return false;
                }

                // Direct property update with immediate save
                user.IsOnline = false;
                user.LastActive = DateTime.UtcNow;

                // Use a very short timeout for logout operations
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogDebug("Successfully set user {UserId} offline for logout", userId);
                    return true;
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to set user {UserId} offline during logout: {Errors}", userId, errors);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fast offline update for user {UserId}", userId);
                return false;
            }
        }

        // NEW: Semaphore-based user update mechanism to prevent concurrency conflicts
        private async Task<bool> UpdateUserWithSemaphoreAsync(string userId, Action<AppUser> updateAction, int maxRetries = 3)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UpdateUserWithSemaphoreAsync called with null or empty userId");
                return false;
            }

            // Get or create semaphore for this user
            SemaphoreSlim userSemaphore;
            await _semaphoreManagerLock.WaitAsync();
            try
            {
                if (!_userUpdateSemaphores.TryGetValue(userId, out userSemaphore))
                {
                    userSemaphore = new SemaphoreSlim(1, 1);
                    _userUpdateSemaphores[userId] = userSemaphore;
                }
            }
            finally
            {
                _semaphoreManagerLock.Release();
            }

            // Use semaphore to ensure only one update per user at a time
            await userSemaphore.WaitAsync(TimeSpan.FromSeconds(5)); // 5 second timeout
            try
            {
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        // Get fresh user data for each attempt
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user == null)
                        {
                            _logger.LogWarning("User {UserId} not found during update attempt {Attempt}", userId, attempt + 1);
                            return false;
                        }

                        // Apply the update
                        updateAction(user);

                        // Try to save with timeout
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var result = await _userManager.UpdateAsync(user);

                        if (result.Succeeded)
                        {
                            _logger.LogDebug("Successfully updated user {UserId} on attempt {Attempt}", userId, attempt + 1);
                            return true;
                        }
                        else
                        {
                            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                            _logger.LogWarning("Failed to update user {UserId} on attempt {Attempt}: {Errors}", userId, attempt + 1, errors);

                            if (attempt == maxRetries - 1)
                            {
                                return false;
                            }

                            // Wait before retry
                            await Task.Delay(100 * (attempt + 1));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (attempt < maxRetries - 1)
                        {
                            _logger.LogWarning(ex, "Error updating user {UserId} on attempt {Attempt}, retrying...", userId, attempt + 1);
                            await Task.Delay(100 * (attempt + 1));
                            continue;
                        }
                        else
                        {
                            _logger.LogError(ex, "Max retries exceeded for user {UserId} due to errors", userId);
                            throw;
                        }
                    }
                }

                return false;
            }
            finally
            {
                userSemaphore.Release();

                // Clean up semaphore if no one is waiting
                await _semaphoreManagerLock.WaitAsync();
                try
                {
                    if (userSemaphore.CurrentCount == 1) // No one waiting
                    {
                        _userUpdateSemaphores.Remove(userId);
                        userSemaphore.Dispose();
                    }
                }
                finally
                {
                    _semaphoreManagerLock.Release();
                }
            }
        }



        // Cleanup method for disposing semaphores
        public void Dispose()
        {
            foreach (var semaphore in _userUpdateSemaphores.Values)
            {
                semaphore?.Dispose();
            }
            _userUpdateSemaphores.Clear();
            _semaphoreManagerLock?.Dispose();
        }
    }
}