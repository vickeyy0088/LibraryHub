using LibraryHub.DTOs.Auth;
using LibraryHub.Shared;

namespace LibraryHub.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<object?>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<object?>> RefreshTokensAsync(string refreshToken);
    Task<ApiResponse<bool>> LogoutAsync(string refreshToken);
    Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordRequestDto request);
    Task<ApiResponse<object?>> GetCurrentUserAsync(string userId);
}
