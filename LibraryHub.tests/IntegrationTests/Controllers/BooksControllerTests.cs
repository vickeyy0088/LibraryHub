using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Book;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Controllers;

public class BooksControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BooksControllerTests(CustomWebApplicationFactory factory)
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

    // Helper method to seed an author and category because every book must have both relationships
    private async Task<(int AuthorId, int CategoryId)> SeedAuthorAndCategoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var author = new Author
        {
            FirstName = "George",
            LastName = "Orwell",
            Biography = "English novelist"
        };
        var category = new Category
        {
            Name = $"Fiction-{Guid.NewGuid():N}",
            Description = "Fiction books"
        };

        db.Authors.Add(author);
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (author.Id, category.Id);
    }

    // Helper method to seed a complete book for update and delete tests
    private async Task<int> SeedBookAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var author = new Author
        {
            FirstName = "Jane",
            LastName = "Austen",
            Biography = "English novelist"
        };
        var category = new Category
        {
            Name = $"Classic-{Guid.NewGuid():N}",
            Description = "Classic books"
        };
        db.Authors.Add(author);
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var book = new Book
        {
            Title = "Pride and Prejudice",
            ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
            Price = 19.99m,
            TotalCopies = 5,
            AvailableCopies = 5,
            AuthorId = author.Id,
            CategoryId = category.Id
        };
        db.Books.Add(book);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return book.Id;
    }

    // 1. GET /api/books (Public endpoint)
    [Fact]
    public async Task GetAll_ShouldReturn200Ok_WhenNoTokenProvided()
    {
        // Arrange (Books GET endpoint is public)
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/books?PageNumber=1&PageSize=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PaginationResponseDto<BookResponseDto>>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // 2. GET /api/books/{id} (Public endpoint)
    [Fact]
    public async Task GetById_ShouldReturn200Ok_WhenBookExists()
    {
        // Arrange: Seed a book directly into the InMemory DbContext
        var seededBookId = await SeedBookAsync();
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync($"/api/books/{seededBookId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<BookResponseDto?>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Title.Should().Be("Pride and Prejudice");
    }

    // 3. POST /api/books (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task Create_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var (authorId, categoryId) = await SeedAuthorAndCategoryAsync();
        var dto = new BookCreateDto
        {
            Title = "Animal Farm",
            ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
            Price = 14.99m,
            TotalCopies = 3,
            AuthorId = authorId,
            CategoryId = categoryId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/books", dto, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn403Forbidden_WhenUserIsMember()
    {
        // Arrange: Member is NOT allowed to create books
        var client = CreateClientWithRole("Member");
        var (authorId, categoryId) = await SeedAuthorAndCategoryAsync();
        var dto = new BookCreateDto
        {
            Title = "Animal Farm",
            ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
            Price = 14.99m,
            TotalCopies = 3,
            AuthorId = authorId,
            CategoryId = categoryId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/books", dto, cancellationToken: TestContext.Current.CancellationToken);

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
        var (authorId, categoryId) = await SeedAuthorAndCategoryAsync();
        var dto = new BookCreateDto
        {
            Title = "The Hobbit",
            ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
            Price = 24.99m,
            TotalCopies = 4,
            AuthorId = authorId,
            CategoryId = categoryId
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/books", dto, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookResponseDto>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Title.Should().Be("The Hobbit");
    }

    // 4. PUT /api/books/{id} (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task Update_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var seededBookId = await SeedBookAsync();
        var dto = new BookUpdateDto
        {
            Title = "Updated Book",
            ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
            Price = 29.99m,
            TotalCopies = 6,
            AuthorId = 1,
            CategoryId = 1
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/books/{seededBookId}", dto, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ShouldReturn403Forbidden_WhenUserIsMember()
    {
        // Arrange: Member is NOT allowed to update books
        var client = CreateClientWithRole("Member");
        var seededBookId = await SeedBookAsync();
        var dto = new BookUpdateDto
        {
            Title = "Updated Book",
            ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
            Price = 29.99m,
            TotalCopies = 6,
            AuthorId = 1,
            CategoryId = 1
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/books/{seededBookId}", dto, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Librarian")]
    [InlineData("Admin")]
    public async Task Update_ShouldReturn200Ok_WhenUserIsLibrarianOrAdmin(string role)
    {
        // Arrange: Seed the related entities and book directly into the InMemory DbContext
        int seededBookId;
        int authorId;
        int categoryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var author = new Author { FirstName = "Leo", LastName = "Tolstoy" };
            var category = new Category { Name = $"Literature-{Guid.NewGuid():N}" };
            db.Authors.Add(author);
            db.Categories.Add(category);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            authorId = author.Id;
            categoryId = category.Id;

            var book = new Book
            {
                Title = "War and Peace",
                ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
                Price = 20m,
                TotalCopies = 5,
                AvailableCopies = 5,
                AuthorId = authorId,
                CategoryId = categoryId
            };
            db.Books.Add(book);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            seededBookId = book.Id;
        }

        var client = CreateClientWithRole(role);
        var dto = new BookUpdateDto
        {
            Title = "War and Peace - Updated",
            ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
            Price = 25m,
            TotalCopies = 7,
            AuthorId = authorId,
            CategoryId = categoryId
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/books/{seededBookId}", dto, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookResponseDto>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Title.Should().Be("War and Peace - Updated");
    }

    // 5. DELETE /api/books/{id} (Authorize Roles = Admin)
    [Fact]
    public async Task Delete_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var seededBookId = await SeedBookAsync();

        // Act
        var response = await client.DeleteAsync($"/api/books/{seededBookId}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Librarian")]
    public async Task Delete_ShouldReturn403Forbidden_WhenUserIsNotAdmin(string role)
    {
        // Arrange: Neither Member nor Librarian can delete books
        var client = CreateClientWithRole(role);
        var seededBookId = await SeedBookAsync();

        // Act
        var response = await client.DeleteAsync($"/api/books/{seededBookId}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldReturn200Ok_WhenUserIsAdmin()
    {
        // Arrange: Seed a book directly into the InMemory DbContext
        var seededBookId = await SeedBookAsync();
        var client = CreateClientWithRole("Admin");

        // Act
        var response = await client.DeleteAsync($"/api/books/{seededBookId}", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }
}
