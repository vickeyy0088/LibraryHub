using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Helpers;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.AuthorServiceTests;

public class AuthorRepository_DeleteAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthorRepository>> _loggerMock;
    private readonly AuthorRepository _authorRepository;

    public AuthorRepository_DeleteAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<AuthorRepository>>();
        _authorRepository = new AuthorRepository(_dbContext, _loggerMock.Object, new Mock<ICacheHelper>().Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-25)]
    public async Task DeleteAsync_ShouldFail_WhenIdIsZeroOrNegative(int invalidId)
    {
        // Act
        var result = await _authorRepository.DeleteAsync(invalidId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidId);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenAuthorDoesNotExist()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _authorRepository.DeleteAsync(nonExistentId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Authors.NotFound);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldSucceed_WhenAuthorExists()
    {
        // Arrange
        var author = new Author
        {
            Id = 1,
            FirstName = "Leo",
            LastName = "Tolstoy",
            Biography = "Russian writer"
        };

        await _dbContext.Authors.AddAsync(author);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authorRepository.DeleteAsync(author.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Authors.Deleted);
        result.Data.Should().BeTrue();

        // Verify removed from DB
        var deletedAuthor = await _dbContext.Authors.FindAsync(author.Id);
        deletedAuthor.Should().NotBeNull();
        deletedAuthor.IsDeleted.Should().BeTrue();
        deletedAuthor.DeletedAt.Should().NotBeNull();
        deletedAuthor.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        deletedAuthor.Id.Should().Be(author.Id);
        deletedAuthor.FirstName.Should().Be(author.FirstName);
        deletedAuthor.LastName.Should().Be(author.LastName);
        deletedAuthor.Biography.Should().Be(author.Biography);
        deletedAuthor.Books.Should().BeEmpty();
        deletedAuthor.CreatedAt.Should().BeCloseTo(author.CreatedAt, TimeSpan.FromSeconds(2));
    }
}