using FluentAssertions;
using LibraryHub.DTOs.Pagination;
using LibraryHub.DTOs.Report;
using LibraryHub.Shared;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Controllers;

public class ReportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReportsControllerTests(CustomWebApplicationFactory factory)
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

    // 1. GET /api/reports/popular-books (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task PopularBooks_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/reports/popular-books?PageNumber=1&PageSize=10&Limit=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PopularBooks_ShouldReturn403Forbidden_WhenUserIsMember()
    {
        // Arrange: Members cannot access staff reports
        var client = CreateClientWithRole("Member");

        // Act
        var response = await client.GetAsync("/api/reports/popular-books?PageNumber=1&PageSize=10&Limit=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Librarian")]
    [InlineData("Admin")]
    public async Task PopularBooks_ShouldReturn200Ok_WhenUserIsStaff(string role)
    {
        // Arrange
        var client = CreateClientWithRole(role);

        // Act
        var response = await client.GetAsync("/api/reports/popular-books?PageNumber=1&PageSize=10&Limit=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginationResponseDto<PopularBookDto>>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    // 2. GET /api/reports/member-activity (Authorize Roles = Admin,Librarian)
    [Fact]
    public async Task MemberActivity_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = CreateClientWithRole(null);

        // Act
        var response = await client.GetAsync("/api/reports/member-activity?PageNumber=1&PageSize=10&Limit=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MemberActivity_ShouldReturn403Forbidden_WhenUserIsMember()
    {
        // Arrange: Members cannot access staff reports
        var client = CreateClientWithRole("Member");

        // Act
        var response = await client.GetAsync("/api/reports/member-activity?PageNumber=1&PageSize=10&Limit=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Librarian")]
    [InlineData("Admin")]
    public async Task MemberActivity_ShouldReturn200Ok_WhenUserIsStaff(string role)
    {
        // Arrange
        var client = CreateClientWithRole(role);

        // Act
        var response = await client.GetAsync("/api/reports/member-activity?PageNumber=1&PageSize=10&Limit=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginationResponseDto<MemberActivityDto>>>(cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }
}
