using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Author;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.AuthorServiceTests;

public class AuthorRepository_CreateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthorRepository>> _loggerMock;
    private readonly AuthorRepository _authorRepository;

    public AuthorRepository_CreateAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<AuthorRepository>>();
        _authorRepository = new AuthorRepository(_dbContext, _loggerMock.Object);
    }

    [Theory]
    [InlineData(null, "Orwell")]
    [InlineData("", "Orwell")]
    [InlineData("   ", "Orwell")]
    [InlineData("George", null)]
    [InlineData("George", "")]
    [InlineData("George", "   ")]
    public async Task CreateAsync_ShouldFail_WhenFirstNameOrLastNameIsMissing(string? firstName, string? lastName)
    {
        // Arrange
        var request = new AuthorCreateDto
        {
            FirstName = firstName!,
            LastName = lastName!,
            Biography = "Sample biography"
        };

        // Act
        var result = await _authorRepository.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Authors.FullNameIsRequired);
        result.Data.Should().BeNull();

        (await _dbContext.Authors.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = new AuthorCreateDto
        {
            FirstName = "Isaac",
            LastName = "Asimov",
            Biography = "American writer and professor of biochemistry"
        };

        // Act
        var result = await _authorRepository.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Authors.Created);
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().BeGreaterThan(0);
        result.Data.FirstName.Should().Be(request.FirstName);
        result.Data.LastName.Should().Be(request.LastName);
        result.Data.Biography.Should().Be(request.Biography);

        // Verify persisted in DB
        var savedAuthor = await _dbContext.Authors.FindAsync(result.Data.Id);
        savedAuthor.Should().NotBeNull();
        savedAuthor!.FirstName.Should().Be(request.FirstName);
        savedAuthor.LastName.Should().Be(request.LastName);
    }
}