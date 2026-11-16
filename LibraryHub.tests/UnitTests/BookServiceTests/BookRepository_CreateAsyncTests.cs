using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Book;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.BookServiceTests;

public class BookRepository_CreateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BookRepository _repository;

    public BookRepository_CreateAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new BookRepository(_dbContext, new Mock<ILogger<BookRepository>>().Object);
    }

    [Theory]
    [InlineData(null, "978-1")]
    [InlineData("", "978-1")]
    [InlineData("   ", "978-1")]
    [InlineData("Book", null)]
    [InlineData("Book", "")]
    [InlineData("Book", "   ")]
    public async Task CreateAsync_ShouldFail_WhenTitleOrIsbnIsMissing(string? title, string? isbn)
    {
        // Arrange
        var request = new BookCreateDto { Title = title!, ISBN = isbn!, AuthorId = 1, CategoryId = 1 };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Books.TitleISBNRequired);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenAuthorOrCategoryDoesNotExist()
    {
        // Arrange
        var request = new BookCreateDto { Title = "Book", ISBN = "978-1", AuthorId = 1, CategoryId = 1 };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Books.AuthorNotFound);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Leo", LastName = "Tolstoy" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.SaveChangesAsync();
        var request = new BookCreateDto { Title = "War and Peace", ISBN = "978-1", Price = 25, TotalCopies = 4, AuthorId = 1, CategoryId = 1 };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Books.Created);
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be(request.Title);
        result.Data.AvailableCopies.Should().Be(request.TotalCopies);
        result.Data.AuthorName.Should().Be("Leo Tolstoy");
        result.Data.CategoryName.Should().Be("Fiction");
        (await _dbContext.Books.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenIsbnAlreadyExists()
    {
        // Arrange
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Leo", LastName = "Tolstoy" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.Books.AddAsync(new Book { Title = "Existing", ISBN = "978-1", AuthorId = 1, CategoryId = 1 });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.CreateAsync(new BookCreateDto { Title = "Another", ISBN = "978-1", AuthorId = 1, CategoryId = 1 });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Books.ISBNExists);
    }
}
