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

public class ReportRepository_GetMemberActivityAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ReportRepository _repository;

    public ReportRepository_GetMemberActivityAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new ReportRepository(_dbContext, new Mock<ILogger<ReportRepository>>().Object);
    }

    [Fact]
    public async Task GetMemberActivityAsync_ShouldFail_WhenPaginationIsInvalid()
    {
        // Arrange
        var query = new PaginationParams { PageNumber = 0, PageSize = 10, Limit = 10 };

        // Act
        var result = await _repository.GetMemberActivityAsync(query);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidPaginationParameters);
    }

    [Fact]
    public async Task GetMemberActivityAsync_ShouldCountOverdueLoans()
    {
        // Arrange
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", FullName = "Reader" });
        await _dbContext.Members.AddAsync(new Member { Id = 1, ApplicationUserId = "user-1", MembershipNumber = "M-1" });
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Author", LastName = "One" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.Books.AddAsync(new Book { Id = 1, Title = "Book", ISBN = "B-1", AuthorId = 1, CategoryId = 1 });
        await _dbContext.Loans.AddAsync(new Loan { Id = 1, BookId = 1, MemberId = 1, DueDate = DateTime.UtcNow.AddDays(-1) });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetMemberActivityAsync(new PaginationParams { PageNumber = 1, PageSize = 10, Limit = 10 });

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Reports.MemberActivity);
        result.Data!.Data.Single().LoanCount.Should().Be(1);
        result.Data.Data.Single().OverdueCount.Should().Be(1);
    }
}
