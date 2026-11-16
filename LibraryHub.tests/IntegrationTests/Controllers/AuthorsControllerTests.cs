using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Author;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Controllers;

public class AuthorsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorsControllerTests(CustomWebApplicationFactory factory)
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

    // 1. GET /api/authors (Authorize - Any Role)
    [Fact]
    public async Task GetAll_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange (Anonymous client - no Authorization header)
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/authors", TestContext.Current.CancellationToken);

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
        var response = await client.GetAsync("/api/authors?PageNumber=1&PageSize=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PaginationResponseDto<AuthorResponseDto>>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // 2. POST /api/authors (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task Create_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        var client = CreateClientWithRole(null);
        var dto = new AuthorCreateDto { FirstName = "George", LastName = "Orwell" };

        var response = await client.PostAsJsonAsync("/api/authors", dto, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn403Forbidden_WhenUserIsMember()
    {
        // Arrange: Member is NOT allowed to create authors
        var client = CreateClientWithRole("Member");
        var dto = new AuthorCreateDto { FirstName = "George", LastName = "Orwell" };

        // Act
        var response = await client.PostAsJsonAsync("/api/authors", dto, cancellationToken: TestContext.Current.CancellationToken);

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
        var dto = new AuthorCreateDto
        {
            FirstName = "J.R.R.",
            LastName = "Tolkien",
            Biography = "Author of The Lord of the Rings"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/authors", dto, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthorResponseDto>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.FirstName.Should().Be("J.R.R.");
    }

    // 3. DELETE /api/authors/{id} (Authorize Roles = Admin)
    [Fact]
    public async Task Delete_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        var client = CreateClientWithRole(null);

        var response = await client.DeleteAsync("/api/authors/1", cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Librarian")]
    public async Task Delete_ShouldReturn403Forbidden_WhenUserIsNotAdmin(string role)
    {
        // Arrange: Neither Member nor Librarian can delete
        var client = CreateClientWithRole(role);

        // Act
        var response = await client.DeleteAsync("/api/authors/1", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldReturn200Ok_WhenUserIsAdmin()
    {
        // Arrange: Seed an author directly into the InMemory DbContext
        int seededAuthorId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var author = new Author
            {
                FirstName = "Agatha",
                LastName = "Christie",
                Biography = "Mystery writer"
            };
            db.Authors.Add(author);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            seededAuthorId = author.Id;
        }

        var client = CreateClientWithRole("Admin");

        // Act
        var response = await client.DeleteAsync($"/api/authors/{seededAuthorId}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().BeTrue();
    }
}