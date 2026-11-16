using FluentAssertions;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LibraryHub.Tests.UnitTests.UserServiceTests;

public class UserRepository_GetAllAsyncTests
{
    [Fact]
    public async Task GetAllAsync_ShouldFail_WhenPaginationIsInvalid()
    {
        // Arrange
        var userManager = CreateUserManager();
        var roleManager = CreateRoleManager();
        var repository = new UserRepository(userManager.Object, roleManager.Object);
        var query = new PaginationParams { PageNumber = 0, PageSize = 10 };

        // Act
        var result = await repository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidPaginationParameters);
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
