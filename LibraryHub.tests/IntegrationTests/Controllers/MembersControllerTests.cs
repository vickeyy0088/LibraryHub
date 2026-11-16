using FluentAssertions;
using LibraryHub.DTOs.Member;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Controllers;

public class MembersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MembersControllerTests(CustomWebApplicationFactory factory)
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

    // Helper method to seed an ApplicationUser because every member belongs to a user
    private async Task<string> SeedUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = $"member-{Guid.NewGuid():N}@libraryhub.com",
            Email = $"member-{Guid.NewGuid():N}@libraryhub.com",
            FullName = "Integration Test Member"
        };
        var result = await userManager.CreateAsync(user, "Password@123");
        result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(error => error.Description)));
        return user.Id;
    }

    // 1. GET /api/members (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task GetAll_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/members", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Librarian")]
    [InlineData("Admin")]
    public async Task GetById_ShouldReturn200OkWithFailure_WhenAuthenticated(string role)
    {
        // Arrange: Any authenticated role may access the endpoint
        var client = CreateClientWithRole(role);

        // Act
        var response = await client.GetAsync("/api/members/999999", TestContext.Current.CancellationToken);

        // Assert: The controller allows the request through; repository returns an unsuccessful response.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MemberResponseDto?>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
    }

    // 2. POST /api/members (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task Create_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var request = new MemberCreateDto
        {
            MembershipNumber = $"MEM-{Guid.NewGuid():N}"[..20],
            ApplicationUserId = "missing-user"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/members", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn403Forbidden_WhenUserIsMember()
    {
        // Arrange: Member is NOT allowed to create members
        var client = CreateClientWithRole("Member");
        var request = new MemberCreateDto
        {
            MembershipNumber = $"MEM-{Guid.NewGuid():N}"[..20],
            ApplicationUserId = "missing-user"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/members", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Librarian")]
    [InlineData("Admin")]
    public async Task Create_ShouldReturn200Ok_WhenUserIsLibrarianOrAdmin(string role)
    {
        // Arrange
        var client = CreateClientWithRole(role);
        var userId = await SeedUserAsync();
        var request = new MemberCreateDto
        {
            MembershipNumber = $"MEM-{Guid.NewGuid():N}"[..20],
            ApplicationUserId = userId,
            IsActive = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/members", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MemberResponseDto>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.ApplicationUserId.Should().Be(userId);
    }

    // 3. DELETE /api/members/{id} (Authorize Roles = Admin)
    [Fact]
    public async Task Delete_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.DeleteAsync("/api/members/1", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Librarian")]
    public async Task Delete_ShouldReturn403Forbidden_WhenUserIsNotAdmin(string role)
    {
        // Arrange: Neither Member nor Librarian can delete members
        var client = CreateClientWithRole(role);

        // Act
        var response = await client.DeleteAsync("/api/members/1", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
