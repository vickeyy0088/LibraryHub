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

public class AuthService_RegisterAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;
    public AuthService_RegisterAsyncTests()
    {
        _userManagerMock = MockUserManager();
        _roleManagerMock = MockRoleManager();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        // In-memory Database setup for isolation
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
    public async Task RegisterAsync_ShouldFail_WHenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = "test@example.com",
            Password = "Password123!",
            FullName = "Test User",
            Role = "Member"
        };

        _userManagerMock
            .Setup(a => a.FindByEmailAsync(request.Email))
            .ReturnsAsync(new ApplicationUser { Email = request.Email });

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.EmailAlreadyExists);
        result.Data.Should().BeNull();

        _userManagerMock.Verify(a => a.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenRoleIsInvalid()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = "user@example.com",
            Password = "Password123!",
            FullName = "Test User",
            Role = "Admin" // Fails since AuthService hardcodes allowed role to "Member"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.InvalidRole);
        result.Data.Should().BeNull();

        _roleManagerMock.Verify(x => x.RoleExistsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenRoleDoesNotExistInRoleManager()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = "user@example.com",
            Password = "Password123!",
            FullName = "Test User",
            Role = "Member"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync("Member"))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.RoleNotFound);
        result.Data.Should().BeNull();

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenUserCreationFails()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = "user@example.com",
            Password = "WeakPassword",
            FullName = "Test User",
            Role = "Member"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync("Member"))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooWeak" }));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.RegistrationFailed);
        result.Data.Should().BeNull();

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldRollbackAndFail_WhenRoleAssignmentFails()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = "user@example.com",
            Password = "Password123!",
            FullName = "Test User",
            Role = "Member"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync("Member"))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Member"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "RoleAssignmentFailed" }));

        _userManagerMock
            .Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Auth.RegistrationFailed);
        result.Data.Should().BeNull();

        // Verify that user deletion (rollback) was triggered
        _userManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSucceed_WhenDataIsValid()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FullName = "New User",
            Role = "Member"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync("Member"))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Member"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Auth.RegisterSuccess);
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(request.Email);
        result.Data.FullName.Should().Be(request.FullName);
        result.Data.Role.Should().Be("Member");
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
