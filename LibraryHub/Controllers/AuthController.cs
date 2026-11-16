using LibraryHub.DTOs.Auth;
using LibraryHub.Interfaces;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService service) : ControllerBase
{
    private readonly IAuthService _service = service;
    [HttpPost("register")]
    [EnableRateLimiting("RateLimit")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        return Ok(await _service.RegisterAsync(request));
    }

    [HttpPost("login")]
    [EnableRateLimiting("RateLimit")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        return Ok(await _service.LoginAsync(request));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _service.RefreshTokensAsync(request.RefreshToken);
        return Ok(result);
    }


    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
    {
        var result = await _service.LogoutAsync(request.RefreshToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(
                ApiResponse<bool>.Fail(ApiMessage.Common.Unauthorized));
        }

        var result = await _service.ChangePasswordAsync(userId, request);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(
                ApiResponse<object>.Fail(ApiMessage.Common.Unauthorized));
        }

        var result = await _service.GetCurrentUserAsync(userId);
        return Ok(result);
    }
}

