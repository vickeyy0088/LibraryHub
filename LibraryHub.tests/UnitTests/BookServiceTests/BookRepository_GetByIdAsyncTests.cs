using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Helpers;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.BookServiceTests;

public class BookRepository_GetByIdAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BookRepository _repository;

    public BookRepository_GetByIdAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new BookRepository(_dbContext, new Mock<ILogger<BookRepository>>().Object, new Mock<ICacheHelper>().Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_ShouldFail_WhenIdIsInvalid(int id)
    {
        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidId);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFail_WhenBookDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Books.NotFound);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldSucceed_WhenBookExists()
    {
        // Arrange
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Leo", LastName = "Tolstoy" }, TestContext.Current.CancellationToken);
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" }, TestContext.Current.CancellationToken);
        await _dbContext.Books.AddAsync(new Book { Id = 1, Title = "War and Peace", ISBN = "978-1", AuthorId = 1, CategoryId = 1 }, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Books.GetById);
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("War and Peace");
        result.Data.AuthorName.Should().Be("Leo Tolstoy");
        result.Data.CategoryName.Should().Be("Fiction");
    }
}
