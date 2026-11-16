using FluentAssertions;
using LibraryHub.DTOs.Models;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Controllers;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Helper method to create an HttpClient with a specific role (or null for anonymous)
    private HttpClient CreateClientWithRole(string? role)
    {
        var client = _factory.CreateClient();

        if (role != null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        }

        return client;
    }

    // Helper method to seed a user for role-management tests
    private async Task<string> SeedUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = $"user-{Guid.NewGuid():N}@libraryhub.com",
            Email = $"user-{Guid.NewGuid():N}@libraryhub.com",
            FullName = "Integration Test User"
        };
        var result = await userManager.CreateAsync(user, "Password@123");
        result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(error => error.Description)));
        return user.Id;
    }

    // Helper method to ensure the role-management tests use an available Identity role
    private async Task EnsureRoleAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));
            result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(error => error.Description)));
        }
    }

    // 1. GET /api/users (Authorize Role = Admin)
    [Fact]
    public async Task GetAll_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/users", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ShouldReturn403Forbidden_WhenUserIsLibrarian()
    {
        // Arrange: Only Admin users can list users
        var client = CreateClientWithRole("Librarian");

        // Act
        var response = await client.GetAsync("/api/users?PageNumber=1&PageSize=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200Ok_WhenUserIsAdmin()
    {
        // Arrange
        var client = CreateClientWithRole("Admin");

        // Act
        var response = await client.GetAsync("/api/users?PageNumber=1&PageSize=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginationResponseDto<UserResponseDto>>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // 2. POST /api/users/{id}/roles (Authorize Role = Admin)
    [Fact]
    public async Task AssignRole_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var userId = await SeedUserAsync();

        // Act
        var response = await client.PostAsJsonAsync($"/api/users/{userId}/roles", new { Role = "Member" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignRole_ShouldReturn403Forbidden_WhenUserIsLibrarian()
    {
        // Arrange: Only Admin users can manage roles
        var client = CreateClientWithRole("Librarian");
        var userId = await SeedUserAsync();

        // Act
        var response = await client.PostAsJsonAsync($"/api/users/{userId}/roles", new { Role = "Member" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_ShouldReturn200Ok_WhenUserIsAdmin()
    {
        // Arrange
        var client = CreateClientWithRole("Admin");
        var userId = await SeedUserAsync();
        await EnsureRoleAsync("Member");

        // Act
        var response = await client.PostAsJsonAsync($"/api/users/{userId}/roles", new { Role = "Member" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // 3. DELETE /api/users/{id}/roles/{role} (Authorize Role = Admin)
    [Fact]
    public async Task RemoveRole_ShouldReturn200Ok_WhenUserIsAdmin()
    {
        // Arrange: Assign the role first so removal has a real role to remove
        var client = CreateClientWithRole("Admin");
        var userId = await SeedUserAsync();
        await EnsureRoleAsync("Member");
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);
            var result = await userManager.AddToRoleAsync(user!, "Member");
            result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        // Act
        var response = await client.DeleteAsync($"/api/users/{userId}/roles/Member", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }
}
