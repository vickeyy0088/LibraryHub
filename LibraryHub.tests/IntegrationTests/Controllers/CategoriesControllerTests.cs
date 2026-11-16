using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Category;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Controllers;

public class CategoriesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CategoriesControllerTests(CustomWebApplicationFactory factory)
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

    // 1. GET /api/categories (Authorize - Any Role)
    [Fact]
    public async Task GetAll_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange (Anonymous client - no Authorization header)
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/categories", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Librarian")]
    [InlineData("Admin")]
    public async Task GetAll_ShouldReturn200Ok_ForAnyAuthenticatedRole(string role)
    {
        // Arrange
        var client = CreateClientWithRole(role);

        // Act
        var response = await client.GetAsync("/api/categories?PageNumber=1&PageSize=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PaginationResponseDto<CategoryResponseDto>>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // 2. POST /api/categories (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task Create_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var dto = new CategoryCreateDto { Name = $"Science-{Guid.NewGuid():N}"[..20], Description = "Science books" };

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", dto, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn403Forbidden_WhenUserIsMember()
    {
        // Arrange: Member is NOT allowed to create categories
        var client = CreateClientWithRole("Member");
        var dto = new CategoryCreateDto { Name = $"Science-{Guid.NewGuid():N}"[..20], Description = "Science books" };

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", dto, cancellationToken: TestContext.Current.CancellationToken);

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
        var dto = new CategoryCreateDto
        {
            Name = $"History-{Guid.NewGuid():N}"[..20],
            Description = "History books"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", dto, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryResponseDto>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Description.Should().Be("History books");
    }

    // 3. DELETE /api/categories/{id} (Authorize Roles = Admin)
    [Fact]
    public async Task Delete_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var categoryId = await SeedCategoryAsync();

        // Act
        var response = await client.DeleteAsync($"/api/categories/{categoryId}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Librarian")]
    public async Task Delete_ShouldReturn403Forbidden_WhenUserIsNotAdmin(string role)
    {
        // Arrange: Neither Member nor Librarian can delete categories
        var client = CreateClientWithRole(role);
        var categoryId = await SeedCategoryAsync();

        // Act
        var response = await client.DeleteAsync($"/api/categories/{categoryId}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldReturn200Ok_WhenUserIsAdmin()
    {
        // Arrange: Seed a category directly into the InMemory DbContext
        var categoryId = await SeedCategoryAsync();
        var client = CreateClientWithRole("Admin");

        // Act
        var response = await client.DeleteAsync($"/api/categories/{categoryId}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // Helper method to seed a category for delete tests
    private async Task<int> SeedCategoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new Category { Name = $"Seeded-{Guid.NewGuid():N}"[..20] };
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category.Id;
    }
}
