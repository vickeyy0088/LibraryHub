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

public class AuthService_ChangePasswordAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthService_ChangePasswordAsyncTests()
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

    [Fact]
    public async Task ChangePasswordAsync_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var userId = "non-existent-user-id";
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.ChangePasswordAsync(userId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.UserNotFound);
        result.Data.Should().BeFalse();

        _userManagerMock.Verify(x => x.ChangePasswordAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldFail_WhenChangePasswordFails()
    {
        // Arrange
        var userId = "user-123";
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "WrongOldPassword",
            NewPassword = "NewPassword123!"
        };

        var user = new ApplicationUser { Id = userId, Email = "user@example.com" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch" }));

        // Act
        var result = await _authService.ChangePasswordAsync(userId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.PasswordChangeFailed);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldSucceed_WhenPasswordChangedSuccessfully()
    {
        // Arrange
        var userId = "user-123";
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "CorrectOldPassword123!",
            NewPassword = "NewPassword123!"
        };

        var user = new ApplicationUser { Id = userId, Email = "user@example.com" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.ChangePasswordAsync(userId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Auth.PasswordChanged);
        result.Data.Should().BeTrue();
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // UserManager requires 9 parameters in its constructor, so we provide null for the optional services.
        // pass null!(null - forgiving operator to silence compiler warnings)
    }

    private static Mock<RoleManager<IdentityRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object, null!, null!, null!, null!);
    }
}
