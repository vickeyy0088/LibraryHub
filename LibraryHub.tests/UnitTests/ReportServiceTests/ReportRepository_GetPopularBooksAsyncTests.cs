using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.ReportServiceTests;

public class ReportRepository_GetPopularBooksAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ReportRepository _repository;

    public ReportRepository_GetPopularBooksAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new ReportRepository(_dbContext, new Mock<ILogger<ReportRepository>>().Object);
    }

    [Fact]
    public async Task GetPopularBooksAsync_ShouldFail_WhenLimitIsInvalid()
    {
        // Arrange
        var query = new PaginationParams { PageNumber = 1, PageSize = 10, Limit = 0 };

        // Act
        var result = await _repository.GetPopularBooksAsync(query);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Reports.InvalidLimit);
    }

    [Fact]
    public async Task GetPopularBooksAsync_ShouldOrderBooksByBorrowCount()
    {
        // Arrange
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Author", LastName = "One" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.Books.AddRangeAsync(new Book { Id = 1, Title = "Popular", ISBN = "P-1", TotalCopies = 3, AuthorId = 1, CategoryId = 1 }, new Book { Id = 2, Title = "Other", ISBN = "P-2", AuthorId = 1, CategoryId = 1 });
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", FullName = "Reader" });
        await _dbContext.Members.AddAsync(new Member { Id = 1, ApplicationUserId = "user-1", MembershipNumber = "M-1" });
        await _dbContext.Loans.AddRangeAsync(new Loan { Id = 1, BookId = 1, MemberId = 1 }, new Loan { Id = 2, BookId = 1, MemberId = 1 });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetPopularBooksAsync(new PaginationParams { PageNumber = 1, PageSize = 10, Limit = 10 });

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Reports.PopularBooks);
        result.Data!.Data.First().Title.Should().Be("Popular");
        result.Data.Data.First().BorrowCount.Should().Be(2);
    }
}
