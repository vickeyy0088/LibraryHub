using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Book;
using LibraryHub.Helpers;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.BookServiceTests;

public class BookRepository_UpdateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BookRepository _repository;

    public BookRepository_UpdateAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new BookRepository(_dbContext, new Mock<ILogger<BookRepository>>().Object, new Mock<ICacheHelper>().Object);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenIdIsInvalidOrBookDoesNotExist()
    {
        // Arrange
        var request = new BookUpdateDto { Title = "Updated", ISBN = "978-2", AuthorId = 1, CategoryId = 1, TotalCopies = 2 };

        // Act
        var invalidIdResult = await _repository.UpdateAsync(0, request);
        var missingResult = await _repository.UpdateAsync(999, request);

        // Assert
        invalidIdResult.Message.Should().Be(ApiMessage.Common.InvalidId);
        missingResult.Message.Should().Be(ApiMessage.Books.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenTitleOrIsbnIsMissing()
    {
        // Arrange
        await SeedAsync();
        var request = new BookUpdateDto { Title = "", ISBN = "978-2", AuthorId = 1, CategoryId = 1, TotalCopies = 2 };

        // Act
        var result = await _repository.UpdateAsync(1, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Books.TitleISBNRequired);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        await SeedAsync();
        var request = new BookUpdateDto { Title = "Updated", ISBN = "978-2", Price = 30, TotalCopies = 5, AuthorId = 1, CategoryId = 1, };
        // Act
        var result = await _repository.UpdateAsync(1, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Books.Updated);
        result.Data!.Title.Should().Be("Updated");
        result.Data.AvailableCopies.Should().Be(5);
        (await _dbContext.Books.FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken))!.Title.Should().Be("Updated");
    }

    private async Task SeedAsync()
    {
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Leo", LastName = "Tolstoy" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.Books.AddAsync(new Book { Id = 1, Title = "Original", ISBN = "978-1", TotalCopies = 2, AvailableCopies = 2, AuthorId = 1, CategoryId = 1 });
        await _dbContext.SaveChangesAsync();
    }
}
