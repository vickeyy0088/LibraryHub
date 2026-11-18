using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Author;
using LibraryHub.Helpers;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.AuthorServiceTests;

public class AuthorRepository_UpdateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthorRepository>> _loggerMock;
    private readonly AuthorRepository _authorRepository;

    public AuthorRepository_UpdateAsyncTests()
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
    [InlineData(-50)]
    public async Task UpdateAsync_ShouldFail_WhenIdIsZeroOrNegative(int invalidId)
    {
        // Arrange
        var request = new AuthorUpdateDto
        {
            FirstName = "Updated",
            LastName = "Author",
            Biography = "Updated bio"
        };

        // Act
        var result = await _authorRepository.UpdateAsync(invalidId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidId);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenAuthorDoesNotExist()
    {
        // Arrange
        var nonExistentId = 999;
        var request = new AuthorUpdateDto
        {
            FirstName = "Updated",
            LastName = "Author",
            Biography = "Updated bio"
        };

        // Act
        var result = await _authorRepository.UpdateAsync(nonExistentId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Authors.NotFound);
        result.Data.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Author")]
    [InlineData("", "Author")]
    [InlineData("   ", "Author")]
    [InlineData("Updated", null)]
    [InlineData("Updated", "")]
    [InlineData("Updated", "   ")]
    public async Task UpdateAsync_ShouldFail_WhenFirstNameOrLastNameIsMissing(string? firstName, string? lastName)
    {
        // Arrange
        var existingAuthor = new Author
        {
            Id = 1,
            FirstName = "Original",
            LastName = "Name",
            Biography = "Original bio"
        };

        await _dbContext.Authors.AddAsync(existingAuthor);
        await _dbContext.SaveChangesAsync();

        var request = new AuthorUpdateDto
        {
            FirstName = firstName!,
            LastName = lastName!,
            Biography = "New bio"
        };

        // Act
        var result = await _authorRepository.UpdateAsync(existingAuthor.Id, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Authors.FullNameIsRequired);
        result.Data.Should().BeNull();

        // Ensure DB record was not modified
        var dbAuthor = await _dbContext.Authors.FindAsync(existingAuthor.Id);
        dbAuthor!.FirstName.Should().Be("Original");
        dbAuthor.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var existingAuthor = new Author
        {
            Id = 1,
            FirstName = "Ernest",
            LastName = "Hemingway",
            Biography = "Old bio"
        };

        await _dbContext.Authors.AddAsync(existingAuthor);
        await _dbContext.SaveChangesAsync();

        var request = new AuthorUpdateDto
        {
            FirstName = "Ernest Miller",
            LastName = "Hemingway",
            Biography = "American novelist, short-story writer, and journalist"
        };

        // Act
        var result = await _authorRepository.UpdateAsync(existingAuthor.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Authors.Updated);
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(existingAuthor.Id);
        result.Data.FirstName.Should().Be(request.FirstName);
        result.Data.LastName.Should().Be(request.LastName);
        result.Data.Biography.Should().Be(request.Biography);

        // Verify persisted changes in DB
        var updatedAuthor = await _dbContext.Authors.FindAsync(existingAuthor.Id);
        updatedAuthor!.FirstName.Should().Be(request.FirstName);
        updatedAuthor.LastName.Should().Be(request.LastName);
        updatedAuthor.Biography.Should().Be(request.Biography);
    }
}