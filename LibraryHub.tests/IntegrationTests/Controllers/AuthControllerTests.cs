using FluentAssertions;
using LibraryHub.DTOs.Auth;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // A helper method that builds an HttpClient pretending to be an already logged-in user
    private HttpClient CreateAuthenticatedClient(string userId, string role = "Member")
    {
        var client = _factory.CreateClient(); // Creates a virtual HTTP client wired directly to your in-memory ASP.NET Core pipeline
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }

    // 1. GET /api/auth/me ([Authorize])
    [Fact]
    public async Task GetCurrentUser_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient(); // Creates a plain, unauthenticated client (no Authorization header)
        // Act
        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturn200Ok_WhenUserExistsAndIsAuthenticated()
    {
        // Arrange: Seed a real ApplicationUser via UserManager
        string seededUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "member1@libraryhub.com",
                Email = "member1@libraryhub.com",
                FullName = "Rahul Sharma"
            };
            await userManager.CreateAsync(user, "Password@123");
            seededUserId = user.Id;
        }

        var client = CreateAuthenticatedClient(seededUserId, "Member");

        // Act
        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // 2. POST /api/auth/change-password ([Authorize])
    [Fact]
    public async Task ChangePassword_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "Password@123",
            NewPassword = "NewPassword@123",
            ConfirmNewPassword = "NewPassword@123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/change-password", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturn200Ok_WhenAuthenticatedUserProvidesValidPasswords()
    {
        // Arrange: Seed user with unique email and verify CreateAsync succeeds
        string seededUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var uniqueEmail = $"changepass_{Guid.NewGuid():N}@libraryhub.com";
            var user = new ApplicationUser
            {
                UserName = uniqueEmail,
                Email = uniqueEmail,
                FullName = "Ashish Verma"
            };

            var createResult = await userManager.CreateAsync(user, "OldPass@123!");
            createResult.Succeeded.Should().BeTrue(
                string.Join(", ", createResult.Errors.Select(e => e.Description)));

            seededUserId = user.Id;
        }

        var client = CreateAuthenticatedClient(seededUserId);
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPass@123!",
            NewPassword = "NewPass@123!",
            ConfirmNewPassword = "NewPass@123!"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            request,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(
            cancellationToken: TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue(body.Message);
    }

    [Fact]
    public async Task Login_ShouldReturn200Ok_WithInvalidCredentialsResponse_WhenUserNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequestDto
        {
            Email = "nonexistent@libraryhub.com",
            Password = "WrongPassword@123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert: Controller returns Ok(ApiResponse.Fail(...))
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldReturn429TooManyRequests_WhenRateLimitExceeded()
    {
        // Arrange: RateLimit policy allows 5 requests per 10 seconds
        // Note: Use a dedicated factory instance so the rate limiter counter starts at 0
        using var isolatedFactory = new CustomWebApplicationFactory();
        var client = isolatedFactory.CreateClient();

        var request = new LoginRequestDto
        {
            Email = "ratelimit@libraryhub.com",
            Password = "Password@123"
        };

        // Act: Send 5 allowed requests, then the 6th request should hit 429
        for (int i = 0; i < 5; i++)
        {
            var okResponse = await client.PostAsJsonAsync("/api/auth/login", request, cancellationToken: TestContext.Current.CancellationToken);
            okResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rateLimitedResponse = await client.PostAsJsonAsync("/api/auth/login", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
