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

public class AuthService_GetCurrentUserAsyncTests // Test class for the GetCurrentUserAsync method in AuthService
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly ApplicationDbContext _dbContext; // In-memory database context for testing
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService; // Instance of the AuthService being tested

    public AuthService_GetCurrentUserAsyncTests()
    {
        _userManagerMock = MockUserManager();
        _roleManagerMock = MockRoleManager();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        // Configures Entity Framework Core to use an In-Memory Database instead of a real SQL Server/Postgres database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())  // Use a unique database name for each test run to avoid conflicts
            .Options;
        _dbContext = new ApplicationDbContext(options);

        //Instantiates the real AuthService and injects .Object (the actual fake instance extracted
        //from each Mock<T> wrapper) along with the in-memory _dbContext
        _authService = new AuthService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _jwtServiceMock.Object,
            _dbContext,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var userId = "non-existent-user-id";

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.UserNotFound);
        result.Data.Should().BeNull();

        _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldSucceed_WhenUserExists()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "john.doe@example.com",
            UserName = "john.doe@example.com",
            FullName = "John Doe"
        };
        var userRoles = new List<string> { "Member" };

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(userRoles);

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Auth.GetUserData);
        result.Data.Should().NotBeNull();

        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
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
