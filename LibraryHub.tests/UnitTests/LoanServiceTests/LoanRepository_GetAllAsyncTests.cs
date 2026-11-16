using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.LoanServiceTests;

public class LoanRepository_GetAllAsyncTests
{
    private readonly LoanRepository _repository;

    public LoanRepository_GetAllAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new ApplicationDbContext(options);
        _repository = new LoanRepository(context, new Mock<ILogger<LoanRepository>>().Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFail_WhenUserIdIsMissing()
    {
        // Arrange
        var query = new PaginationParams { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _repository.GetAllAsync(null, false, query);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Loans.UserIdMissing);
    }
}
