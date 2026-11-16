using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.AuthServiceTests;

public class AuthService_LogoutAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthService_LogoutAsyncTests()
    {
        _userManagerMock = MockUserManager();
        _roleManagerMock = MockRoleManager();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _authService = new AuthService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _jwtServiceMock.Object,
            _dbContext,
            _loggerMock.Object
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task LogoutAsync_ShouldFail_WhenRefreshTokenIsEmptyOrNull(string? refreshToken)
    {
        // Act
        var result = await _authService.LogoutAsync(refreshToken!);
        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidRefreshToken);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_ShouldFail_WhenRefreshTokenIsAlreadyRevoked()
    {
        // Arrange
        var revokedToken = new RefreshToken
        {
            Id = 1,
            Token = "already-revoked-token",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow.AddHours(-1) // Revoked 1 hour ago
        };

        _dbContext.RefreshTokens.Add(revokedToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.LogoutAsync(revokedToken.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidRefreshToken);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_ShouldFail_WhenRefreshTokenIsExpired()
    {
        // Arrange
        var expiredToken = new RefreshToken
        {
            Id = 2,
            Token = "expired-token",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            RevokedAt = null
        };

        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.LogoutAsync(expiredToken.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidRefreshToken);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_ShouldSucceed_WhenRefreshTokenIsValid()
    {
        // Arrange
        var validToken = new RefreshToken
        {
            Id = 3,
            Token = "valid-active-token",
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        _dbContext.RefreshTokens.Add(validToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.LogoutAsync(validToken.Token);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Auth.LogoutSuccess);
        result.Data.Should().BeTrue();

        // Verify token in DB updated
        var updatedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == validToken.Token);
        updatedToken.Should().NotBeNull();
        updatedToken!.RevokedAt.Should().NotBeNull();
        updatedToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<IdentityRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object, null!, null!, null!, null!);
    }
}
