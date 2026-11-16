using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.LoanServiceTests;

public class LoanRepository_GetOverdueAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly LoanRepository _repository;

    public LoanRepository_GetOverdueAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new LoanRepository(_dbContext, new Mock<ILogger<LoanRepository>>().Object);
    }

    [Fact]
    public async Task GetOverdueAsync_ShouldReturnOnlyUnreturnedOverdueLoans()
    {
        // Arrange
        await _dbContext.Authors.AddAsync(new Author { Id = 1, FirstName = "Author", LastName = "One" });
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.Books.AddAsync(new Book { Id = 1, Title = "Book", ISBN = "B-1", AuthorId = 1, CategoryId = 1 });
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", FullName = "Reader" });
        await _dbContext.Members.AddAsync(new Member { Id = 1, ApplicationUserId = "user-1", MembershipNumber = "M-1" });
        await _dbContext.Loans.AddRangeAsync(
            new Loan { Id = 1, BookId = 1, MemberId = 1, DueDate = DateTime.UtcNow.AddDays(-1), Status = "Issued" },
            new Loan { Id = 2, BookId = 1, MemberId = 1, DueDate = DateTime.UtcNow.AddDays(-1), ReturnDate = DateTime.UtcNow, Status = "Returned" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetOverdueAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Loans.GetOverdueLoans);
        result.Data!.Should().ContainSingle(loan => loan.Id == 1);
    }
}
