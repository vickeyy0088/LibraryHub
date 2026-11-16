using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Loan;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Controllers;

public class LoansControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoansControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Helper method to create an HttpClient with a specific role (or null for anonymous)
    private HttpClient CreateClientWithRole(string? role, string? userId = null)
    {
        var client = _factory.CreateClient();

        if (role != null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);

            if (userId != null)
            {
                client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
            }
        }

        return client;
    }

    // Helper method to seed a book and member because every loan requires both relationships
    private async Task<(int BookId, int MemberId)> SeedBookAndMemberAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var author = new Author
        {
            FirstName = "William",
            LastName = "Shakespeare"
        };
        var category = new Category
        {
            Name = $"Drama-{Guid.NewGuid():N}"[..20]
        };
        db.Authors.Add(author);
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var book = new Book
        {
            Title = "Hamlet",
            ISBN = $"ISBN-{Guid.NewGuid():N}"[..20],
            Price = 18.99m,
            TotalCopies = 5,
            AvailableCopies = 5,
            AuthorId = author.Id,
            CategoryId = category.Id
        };
        var member = new Member
        {
            MembershipNumber = $"MEM-{Guid.NewGuid():N}"[..20],
            ApplicationUserId = $"user-{Guid.NewGuid():N}"
        };
        db.Books.Add(book);
        db.Members.Add(member);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (book.Id, member.Id);
    }

    // Helper method to seed a loan for return tests
    private async Task<int> SeedLoanAsync()
    {
        var (bookId, memberId) = await SeedBookAndMemberAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var loan = new Loan
        {
            BookId = bookId,
            MemberId = memberId,
            LoanDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Status = "Issued"
        };
        var book = await db.Books.FindAsync(bookId);
        book!.AvailableCopies--;
        db.Loans.Add(loan);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return loan.Id;
    }

    // 1. GET /api/loans (Authorize - Any Role)
    [Fact]
    public async Task GetAll_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange (Anonymous client - no Authorization header)
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/loans", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200Ok_WhenUserIsAdmin()
    {
        // Arrange: Staff users can retrieve all loans
        var client = CreateClientWithRole("Admin", "admin-test-user");

        // Act
        var response = await client.GetAsync("/api/loans?PageNumber=1&PageSize=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PaginationResponseDto<LoanResponseDto>>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // 2. POST /api/loans (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task Create_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var (bookId, memberId) = await SeedBookAndMemberAsync();
        var request = new LoanCreateDto
        {
            BookId = bookId,
            MemberId = memberId,
            DueDate = DateTime.UtcNow.AddDays(14)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/loans", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn403Forbidden_WhenUserIsMember()
    {
        // Arrange: Member is NOT allowed to create loans
        var client = CreateClientWithRole("Member");
        var (bookId, memberId) = await SeedBookAndMemberAsync();
        var request = new LoanCreateDto
        {
            BookId = bookId,
            MemberId = memberId,
            DueDate = DateTime.UtcNow.AddDays(14)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/loans", request, cancellationToken: TestContext.Current.CancellationToken);

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
        var (bookId, memberId) = await SeedBookAndMemberAsync();
        var request = new LoanCreateDto
        {
            BookId = bookId,
            MemberId = memberId,
            DueDate = DateTime.UtcNow.AddDays(14)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/loans", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoanResponseDto?>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.BookId.Should().Be(bookId);
    }

    // 3. PUT /api/loans/{id}/return (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task Return_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);
        var loanId = await SeedLoanAsync();

        // Act
        var response = await client.PutAsync($"/api/loans/{loanId}/return", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Librarian")]
    [InlineData("Admin")]
    public async Task Return_ShouldReturn200Ok_WhenUserIsLibrarianOrAdmin(string role)
    {
        // Arrange
        var client = CreateClientWithRole(role);
        var loanId = await SeedLoanAsync();

        // Act
        var response = await client.PutAsync($"/api/loans/{loanId}/return", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().BeTrue();
    }

    // 4. GET /api/loans/overdue (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task GetOverdue_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/loans/overdue", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Librarian")]
    [InlineData("Admin")]
    public async Task GetOverdue_ShouldReturn200Ok_WhenUserIsLibrarianOrAdmin(string role)
    {
        // Arrange
        var client = CreateClientWithRole(role);

        // Act
        var response = await client.GetAsync("/api/loans/overdue", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<LoanResponseDto>>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }
}
