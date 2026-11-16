using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Loan;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.LoanServiceTests;

public class LoanRepository_CreateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly LoanRepository _repository;

    public LoanRepository_CreateAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new LoanRepository(_dbContext, new Mock<ILogger<LoanRepository>>().Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenDueDateIsNotInTheFuture()
    {
        // Arrange
        var request = new LoanCreateDto { BookId = 1, MemberId = 1, DueDate = DateTime.UtcNow.AddDays(-1) };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Loans.DueDateError);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_AndDecreaseAvailableCopies()
    {
        // Arrange
        await SeedAsync();
        var request = new LoanCreateDto { BookId = 1, MemberId = 1, DueDate = DateTime.UtcNow.AddDays(7) };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Loans.Created);
        result.Data!.Status.Should().Be("Issued");
        (await _dbContext.Books.FindAsync(1))!.AvailableCopies.Should().Be(1);
    }

    private async Task SeedAsync()
    {
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", FullName = "Reader", Email = "reader@test.com" });
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Author", LastName = "One" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.Books.AddAsync(new Book { Id = 1, Title = "Book", ISBN = "B-1", TotalCopies = 2, AvailableCopies = 2, AuthorId = 1, CategoryId = 1 });
        await _dbContext.Members.AddAsync(new Member { Id = 1, ApplicationUserId = "user-1", MembershipNumber = "M-1", IsActive = true });
        await _dbContext.SaveChangesAsync();
    }
}
