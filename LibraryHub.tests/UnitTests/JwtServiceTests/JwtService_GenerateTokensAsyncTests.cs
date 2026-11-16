using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LibraryHub.Tests.UnitTests.JwtServiceTests;

public class JwtService_GenerateTokensAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly JwtService _jwtService;

    public JwtService_GenerateTokensAsyncTests()
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
    public async Task GenerateTokensAsync_ShouldCreateTokensAndPersistRefreshToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "reader",
            Email = "reader@test.com"
        };
        var roles = new List<string> { "Member" };

        // Act
        var result = await _jwtService.GenerateTokensAsync(user, roles);

        // Assert
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        token.Claims.Should().Contain(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        token.Claims.Should().Contain(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email && c.Value == user.Email);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Member");

        var storedToken = await _dbContext.RefreshTokens.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        storedToken.Token.Should().Be(result.RefreshToken);
        storedToken.UserId.Should().Be(user.Id);
        storedToken.ExpiresAt.Should().BeCloseTo(result.RefreshTokenExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GenerateTokensAsync_ShouldThrow_WhenJwtConfigurationIsMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var service = new JwtService(
            configuration,
            _dbContext,
            _userManagerMock.Object,
            new Mock<ILogger<JwtService>>().Object);

        // Act
        var action = async () => await service.GenerateTokensAsync(new ApplicationUser { Id = "user-1" }, []);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("JWT Key is not configured.");
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
