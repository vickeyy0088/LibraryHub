
using LibraryHub.Data;
using LibraryHub.DTOs.Auth;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IJwtService jwtService,
    ApplicationDbContext context,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly IJwtService _jwtService = jwtService;
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<ApiResponse<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) != null)
        {
            _logger.LogWarning("Email already exists: {Email}", request.Email);
            return ApiResponse<RegisterResponseDto>.Fail(ApiMessage.Auth.EmailAlreadyExists);
        }

        var role = new[] { "Member" }.FirstOrDefault(r =>
            r.Equals(request.Role, StringComparison.OrdinalIgnoreCase));

        if (role == null)
        {
            _logger.LogWarning("Invalid registration role: {Role}", request.Role);
            return ApiResponse<RegisterResponseDto>.Fail(ApiMessage.Auth.InvalidRole);
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            _logger.LogError("Required role not found: {Role}", role);
            return ApiResponse<RegisterResponseDto>.Fail(ApiMessage.Auth.RoleNotFound);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            _logger.LogWarning("User creation failed for {Email}. Errors: {Errors}",
                request.Email, string.Join(", ", result.Errors.Select(e => e.Code)));

            return ApiResponse<RegisterResponseDto>.Fail(ApiMessage.Auth.RegistrationFailed);
        }

        _logger.LogInformation("User created: {UserId}", user.Id);

        var roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            _logger.LogError("Role assignment failed for {UserId}. Errors: {Errors}",
                user.Id, string.Join(", ", roleResult.Errors.Select(e => e.Code)));

            var deleteResult = await _userManager.DeleteAsync(user);

            if (!deleteResult.Succeeded)
                _logger.LogError("User rollback failed for {UserId}. Errors: {Errors}",
                    user.Id, string.Join(", ", deleteResult.Errors.Select(e => e.Code)));
            else
                _logger.LogInformation("User creation rolled back: {UserId}", user.Id);

            return ApiResponse<RegisterResponseDto>.Fail(ApiMessage.Auth.RegistrationFailed);
        }

        _logger.LogInformation("Role {Role} assigned to {UserId}", role, user.Id);

        var response = new RegisterResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = role
        };

        _logger.LogInformation("Registration completed for {UserId}", user.Id);

        return ApiResponse<RegisterResponseDto>.Ok(ApiMessage.Auth.RegisterSuccess, response);
    }

    public async Task<ApiResponse<object?>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Login failed due to invalid credentials.");
            return ApiResponse<object?>.Fail(ApiMessage.Auth.InvalidCredentials);
        }

        _logger.LogDebug("Password verified for {UserId}", user.Id);

        var roles = await _userManager.GetRolesAsync(user);
        _logger.LogDebug("Retrieved {RoleCount} roles for {UserId}", roles.Count, user.Id);

        var tokens = await _jwtService.GenerateTokensAsync(user, roles);

        _logger.LogInformation("Tokens generated and login successful for {UserId}", user.Id);

        object data = new
        {
            user = new { user.Id, user.FullName, user.Email, Roles = roles },
            tokens
        };

        return ApiResponse<object?>.Ok(ApiMessage.Auth.LoginSuccess, data);
    }

    public async Task<ApiResponse<object?>> RefreshTokensAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Refresh token is empty.");
            return ApiResponse<object?>.Fail(ApiMessage.Auth.InvalidRefreshToken);
        }

        var tokens = await _jwtService.RefreshTokensAsync(refreshToken);

        if (tokens == null)
        {
            _logger.LogWarning("Refresh token validation failed.");
            return ApiResponse<object?>.Fail(ApiMessage.Auth.InvalidRefreshToken);
        }

        _logger.LogInformation("Refresh tokens generated successfully.");

        return ApiResponse<object?>.Ok(
            ApiMessage.Auth.RefreshTokenGenerated, new { tokens });
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Logout failed: empty refresh token.");
            return ApiResponse<bool>.Fail(ApiMessage.Auth.InvalidRefreshToken);
        }

        var token = await _context.RefreshTokens
            .SingleOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || token.RevokedAt != null || token.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Logout failed: invalid, revoked or expired refresh token.");
            return ApiResponse<bool>.Fail(ApiMessage.Auth.InvalidRefreshToken);
        }

        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked for UserId: {UserId}", token.UserId);

        return ApiResponse<bool>.Ok(ApiMessage.Auth.LogoutSuccess, true);
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(
        string userId, ChangePasswordRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("Password change failed: user not found. {UserId}", userId);
            return ApiResponse<bool>.Fail(ApiMessage.Auth.UserNotFound);
        }

        var result = await _userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Password change failed for {UserId}. Errors: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Code)));

            return ApiResponse<bool>.Fail(ApiMessage.Auth.PasswordChangeFailed);
        }

        _logger.LogInformation("Password changed successfully for {UserId}", userId);

        return ApiResponse<bool>.Ok(ApiMessage.Auth.PasswordChanged, true);
    }

    public async Task<ApiResponse<object?>> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return ApiResponse<object?>.Fail(ApiMessage.Auth.UserNotFound);
        }

        var roles = await _userManager.GetRolesAsync(user);

        object data = new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.UserName,
            Roles = roles
        };

        _logger.LogInformation("User details retrieved: {UserId}", userId);

        return ApiResponse<object?>.Ok(ApiMessage.Auth.GetUserData, data);
    }
}
