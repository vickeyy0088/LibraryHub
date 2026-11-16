using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Auth;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.AuthServiceTests;

public class AuthService_RefreshTokensAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthService_RefreshTokensAsyncTests()
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
    public async Task RefreshTokensAsync_ShouldFail_WhenRefreshTokenIsEmptyOrNull(string? refreshToken)
    {
        // Act
        var result = await _authService.RefreshTokensAsync(refreshToken!);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidRefreshToken);
        result.Data.Should().BeNull();

        _jwtServiceMock.Verify(x => x.RefreshTokensAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokensAsync_ShouldFail_WhenJwtServiceReturnsNull()
    {
        // Arrange
        var refreshToken = "invalid-or-expired-token";

        _jwtServiceMock
            .Setup(x => x.RefreshTokensAsync(refreshToken))
            .ReturnsAsync((TokenResponseDto?)null);

        // Act
        var result = await _authService.RefreshTokensAsync(refreshToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidRefreshToken);
        result.Data.Should().BeNull();

        _jwtServiceMock.Verify(x => x.RefreshTokensAsync(refreshToken), Times.Once);
    }

    [Fact]
    public async Task RefreshTokensAsync_ShouldSucceed_WhenRefreshTokenIsValid()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var mockTokens = new TokenResponseDto
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token"
        };

        _jwtServiceMock
            .Setup(x => x.RefreshTokensAsync(refreshToken))
            .ReturnsAsync(mockTokens);

        // Act
        var result = await _authService.RefreshTokensAsync(refreshToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Auth.RefreshTokenGenerated);
        result.Data.Should().NotBeNull();

        _jwtServiceMock.Verify(x => x.RefreshTokensAsync(refreshToken), Times.Once);
    }

    #region Private Identity Mock Helpers
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
    #endregion
}