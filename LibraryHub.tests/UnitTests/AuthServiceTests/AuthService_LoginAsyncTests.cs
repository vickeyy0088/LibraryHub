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

public class AuthService_LoginAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthService_LoginAsyncTests()
    {
        _userManagerMock = MockUserManager();
        _roleManagerMock = MockRoleManager();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        // Configures Entity Framework Core to use an In-Memory Database instead of a real SQL Server/Postgres database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
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

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidCredentials);
        result.Data.Should().BeNull();

        _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenEmailIsInvalid()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        var existingUser = new ApplicationUser
        {
            Id = "user-123",
            Email = request.Email,
            FullName = "Test User"
        };

        _userManagerMock
           .Setup(x => x.FindByEmailAsync(request.Email))
           .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidCredentials);
        result.Data.Should().BeNull();

        _jwtServiceMock.Verify(x => x.GenerateTokensAsync(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenPasswordIsIncorrect()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        var existingUser = new ApplicationUser
        {
            Id = "user-123",
            Email = request.Email,
            FullName = "Test User"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(existingUser, request.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidCredentials);
        result.Data.Should().BeNull();

        _jwtServiceMock.Verify(x => x.GenerateTokensAsync(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldSucceed_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "validuser@example.com",
            Password = "ValidPassword123!"
        };

        var existingUser = new ApplicationUser
        {
            Id = "user-123",
            Email = request.Email,
            FullName = "Valid User"
        };

        var userRoles = new List<string> { "Member" };
        var mockTokens = new TokenResponseDto
        {
            AccessToken = "mock-access-token",
            RefreshToken = "mock-refresh-token"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(existingUser, request.Password))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(existingUser))
            .ReturnsAsync(userRoles);

        _jwtServiceMock
            .Setup(x => x.GenerateTokensAsync(existingUser, userRoles))
            .ReturnsAsync(mockTokens);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Auth.LoginSuccess);
        result.Data.Should().NotBeNull();

        _jwtServiceMock.Verify(x => x.GenerateTokensAsync(existingUser, userRoles), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldHandleUserWithNoRoles()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "noroleuser@example.com",
            Password = "ValidPassword123!"
        };

        var existingUser = new ApplicationUser
        {
            Id = "user-456",
            Email = request.Email,
            FullName = "No Role User"
        };

        var emptyRoles = new List<string>();
        var mockTokens = new TokenResponseDto
        {
            AccessToken = "mock-access-token",
            RefreshToken = "mock-refresh-token"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(existingUser, request.Password))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(existingUser))
            .ReturnsAsync(emptyRoles);

        _jwtServiceMock
            .Setup(x => x.GenerateTokensAsync(existingUser, emptyRoles))
            .ReturnsAsync(mockTokens);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Auth.LoginSuccess);
        result.Data.Should().NotBeNull();

        _jwtServiceMock.Verify(x => x.GenerateTokensAsync(existingUser, emptyRoles), Times.Once);
    }

    #region Private Identity Mock Helpers
    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        // UserManager requires 9 parameters in its constructor, so we provide null for the optional services.
        // pass null!(null - forgiving operator to silence compiler warnings)
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