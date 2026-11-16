using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.JwtServiceTests;

public class JwtService_RefreshTokensAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly JwtService _jwtService;

    public JwtService_RefreshTokensAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _userManagerMock = CreateUserManager();
        _jwtService = new JwtService(
            CreateConfiguration(),
            _dbContext,
            _userManagerMock.Object,
            new Mock<ILogger<JwtService>>().Object);
    }

    [Fact]
    public async Task RefreshTokensAsync_ShouldReturnNull_WhenTokenDoesNotExist()
    {
        // Act
        var result = await _jwtService.RefreshTokensAsync("missing-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokensAsync_ShouldReturnNull_WhenTokenIsExpiredOrRevoked()
    {
        // Arrange
        await _dbContext.RefreshTokens.AddRangeAsync(
            new RefreshToken { Token = "expired-token", UserId = "user-1", ExpiresAt = DateTime.UtcNow.AddMinutes(-1) },
            new RefreshToken { Token = "revoked-token", UserId = "user-1", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var expiredResult = await _jwtService.RefreshTokensAsync("expired-token");
        var revokedResult = await _jwtService.RefreshTokensAsync("revoked-token");

        // Assert
        expiredResult.Should().BeNull();
        revokedResult.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokensAsync_ShouldRotateTokenAndRevokeOriginal_WhenTokenIsValid()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "reader",
            Email = "reader@test.com"
        };
        await _dbContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await _dbContext.RefreshTokens.AddAsync(new RefreshToken
        {
            Token = "valid-token",
            UserId = user.Id,
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        }, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == user.Id)))
            .ReturnsAsync(new List<string> { "Member" });

        // Act
        var result = await _jwtService.RefreshTokensAsync("valid-token");

        // Assert
        result.Should().NotBeNull();
        result!.RefreshToken.Should().NotBe("valid-token");
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        var originalToken = await _dbContext.RefreshTokens.SingleAsync(t => t.Token == "valid-token", TestContext.Current.CancellationToken);
        originalToken.RevokedAt.Should().NotBeNull();
        (await _dbContext.RefreshTokens.CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-test-key-with-at-least-32-chars",
                ["Jwt:Issuer"] = "LibraryHub.Tests",
                ["Jwt:Audience"] = "LibraryHub.Tests.Client",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
