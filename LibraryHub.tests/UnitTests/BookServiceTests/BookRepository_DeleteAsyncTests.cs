using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.BookServiceTests;

public class BookRepository_DeleteAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BookRepository _repository;

    public BookRepository_DeleteAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new BookRepository(_dbContext, new Mock<ILogger<BookRepository>>().Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteAsync_ShouldFail_WhenIdIsInvalid(int id)
    {
        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidId);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenBookDoesNotExist()
    {
        // Act
        var result = await _repository.DeleteAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Books.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_WhenBookExistsWithoutLoans()
    {
        // Arrange
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Leo", LastName = "Tolstoy" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.Books.AddAsync(new Book { Id = 1, Title = "Book", ISBN = "978-1", AuthorId = 1, CategoryId = 1 });
        await _dbContext.SaveChangesAsync();
        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Books.Deleted);
        result.Data.Should().BeTrue();
        var deleted = await _dbContext.Books.IgnoreQueryFilters().SingleAsync(b => b.Id == 1);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
    }
}
