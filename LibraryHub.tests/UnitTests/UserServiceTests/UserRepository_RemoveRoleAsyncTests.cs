using FluentAssertions;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LibraryHub.Tests.UnitTests.UserServiceTests;

public class UserRepository_RemoveRoleAsyncTests
{
    [Fact]
    public async Task RemoveRoleAsync_ShouldSucceed_WhenUserHasRole()
    {
        // Arrange
        var userManager = CreateUserManager();
        var roleManager = CreateRoleManager();
        var repository = new UserRepository(userManager.Object, roleManager.Object);
        var user = new ApplicationUser { Id = "user-1" };
        userManager.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(user);
        roleManager.Setup(x => x.RoleExistsAsync("Member")).ReturnsAsync(true);
        userManager.Setup(x => x.IsInRoleAsync(user, "Member")).ReturnsAsync(true);
        userManager.Setup(x => x.RemoveFromRoleAsync(user, "Member")).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await repository.RemoveRoleAsync("user-1", "Member");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Users.RoleRemoved);
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
