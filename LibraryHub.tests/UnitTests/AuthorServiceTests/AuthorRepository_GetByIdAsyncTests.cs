using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.AuthorServiceTests;

public class AuthorRepository_GetByIdAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthorRepository>> _loggerMock;
    private readonly AuthorRepository _authorRepository;

    public AuthorRepository_GetByIdAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<AuthorRepository>>();
        _authorRepository = new AuthorRepository(_dbContext, _loggerMock.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_ShouldFail_WhenIdIsZeroOrNegative(int invalidId)
    {
        // Act
        var result = await _authorRepository.GetByIdAsync(invalidId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidId);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFail_WhenAuthorDoesNotExist()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _authorRepository.GetByIdAsync(nonExistentId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Authors.NotFound);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldSucceed_WhenAuthorExists()
    {
        // Arrange
        var author = new Author
        {
            Id = 1,
            FirstName = "Arthur",
            LastName = "Conan Doyle",
            Biography = "Creator of Sherlock Holmes"
        };

        await _dbContext.Authors.AddAsync(author);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authorRepository.GetByIdAsync(author.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Authors.GetById);
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(author.Id);
        result.Data.FirstName.Should().Be(author.FirstName);
        result.Data.LastName.Should().Be(author.LastName);
        result.Data.Biography.Should().Be(author.Biography);
    }
}