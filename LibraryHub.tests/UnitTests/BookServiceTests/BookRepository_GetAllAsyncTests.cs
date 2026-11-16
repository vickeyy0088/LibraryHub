using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.BookServiceTests;

public class BookRepository_GetAllAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BookRepository _repository;

    public BookRepository_GetAllAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new BookRepository(_dbContext, new Mock<ILogger<BookRepository>>().Object);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public async Task GetAllAsync_ShouldFail_WhenPaginationIsInvalid(int pageNumber, int pageSize)
    {
        // Arrange
        var query = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };

        // Act
        var result = await _repository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidPaginationParameters);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedBooksAndFilterBySearch()
    {
        // Arrange
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Leo", LastName = "Tolstoy" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.Books.AddRangeAsync(
            new Book { Id = 1, Title = "War and Peace", ISBN = "978-1", AuthorId = 1, CategoryId = 1 },
            new Book { Id = 2, Title = "Anna Karenina", ISBN = "978-2", AuthorId = 1, CategoryId = 1 });
        await _dbContext.SaveChangesAsync();

        var query = new PaginationParams { PageNumber = 1, PageSize = 10, Search = "Peace" };

        // Act
        var result = await _repository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Books.GetAll);
        result.Data!.TotalCount.Should().Be(1);
        result.Data.Data.Single().Title.Should().Be("War and Peace");
    }
}
