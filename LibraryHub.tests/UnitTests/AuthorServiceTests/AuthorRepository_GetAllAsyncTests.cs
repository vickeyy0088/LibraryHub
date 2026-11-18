using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Helpers;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.AuthorServiceTests;

public class AuthorRepository_GetAllAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<AuthorRepository>> _loggerMock;
    private readonly AuthorRepository _authorRepository;

    public AuthorRepository_GetAllAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<AuthorRepository>>();
        _authorRepository = new AuthorRepository(_dbContext, _loggerMock.Object, new Mock<ICacheHelper>().Object);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    public async Task GetAllAsync_ShouldFail_WhenPaginationParametersAreInvalid(int pageNumber, int pageSize)
    {
        // Arrange
        var query = new PaginationParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Act
        var result = await _authorRepository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidPaginationParameters);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedAuthors_WhenParametersAreValid()
    {
        // Arrange
        var authors = new List<Author>
        {
            new() { Id = 1, FirstName = "George", LastName = "Orwell", Biography = "English novelist" },
            new() { Id = 2, FirstName = "Jane", LastName = "Austen", Biography = "English writer" },
            new() { Id = 3, FirstName = "Mark", LastName = "Twain", Biography = "American writer" }
        };

        await _dbContext.Authors.AddRangeAsync(authors);
        await _dbContext.SaveChangesAsync();

        var query = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 2
        };

        // Act
        var result = await _authorRepository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Authors.GetAll);
        result.Data.Should().NotBeNull();
        result.Data!.Data.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterBySearchTerm_WhenSearchIsProvided()
    {
        // Arrange
        var authors = new List<Author>
        {
            new() { Id = 1, FirstName = "J.K.", LastName = "Rowling", Biography = "Harry Potter author" },
            new() { Id = 2, FirstName = "George", LastName = "Martin", Biography = "Game of Thrones author" },
            new() { Id = 3, FirstName = "Agatha", LastName = "Christie", Biography = "Mystery writer" }
        };

        await _dbContext.Authors.AddRangeAsync(authors);
        await _dbContext.SaveChangesAsync();

        var query = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 10,
            Search = "Rowling"
        };

        // Act
        var result = await _authorRepository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Authors.GetAll);
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(1);

        var author = result.Data.Data.Single();
        author.FirstName.Should().Be("J.K.");
        author.LastName.Should().Be("Rowling");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoAuthorsExist()
    {
        // Arrange
        var query = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _authorRepository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Authors.GetAll);
        result.Data.Should().NotBeNull();
        result.Data!.Data.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }
}