using FluentAssertions;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LibraryHub.Tests.UnitTests.UserServiceTests;

public class UserRepository_AssignRoleAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<RoleManager<IdentityRole>> _roleManager;
    private readonly UserRepository _repository;

    public UserRepository_AssignRoleAsyncTests()
    {
        _userManager = CreateUserManager();
        _roleManager = CreateRoleManager();
        _repository = new UserRepository(_userManager.Object, _roleManager.Object);
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldFail_WhenUserIdOrRoleIsMissing()
    {
        // Act
        var invalidUser = await _repository.AssignRoleAsync("", "Member");
        var invalidRole = await _repository.AssignRoleAsync("user-1", "");

        // Assert
        invalidUser.Message.Should().Be(ApiMessage.Common.InvalidId);
        invalidRole.Message.Should().Be(ApiMessage.Users.RoleNotFound);
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldSucceed_WhenUserAndRoleExist()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-1" };
        _userManager.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(user);
        _roleManager.Setup(x => x.RoleExistsAsync("Member")).ReturnsAsync(true);
        _userManager.Setup(x => x.IsInRoleAsync(user, "Member")).ReturnsAsync(false);
        _userManager.Setup(x => x.AddToRoleAsync(user, "Member")).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _repository.AssignRoleAsync("user-1", "Member");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Users.RoleAssigned);
        result.Data.Should().BeTrue();
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<IdentityRole>> CreateRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
    }
}
