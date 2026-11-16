using LibraryHub.DTOs.Auth;
using LibraryHub.Models;

namespace LibraryHub.Interfaces;

public interface IJwtService
{
    Task<TokenResponseDto> GenerateTokensAsync(ApplicationUser user, IList<string> roles);
    Task<TokenResponseDto?> RefreshTokensAsync(string refreshToken);
}
