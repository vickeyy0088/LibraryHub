using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.CategoryServiceTests;

public class CategoryRepository_GetAllAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CategoryRepository _repository;

    public CategoryRepository_GetAllAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new CategoryRepository(_dbContext, new Mock<ILogger<CategoryRepository>>().Object);
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
    public async Task GetAllAsync_ShouldReturnPaginatedCategoriesAndFilterBySearch()
    {
        // Arrange
        await _dbContext.Categories.AddRangeAsync(
            new Category { Id = 1, Name = "Fiction" },
            new Category { Id = 2, Name = "Science" },
            new Category { Id = 3, Name = "History" });
        await _dbContext.SaveChangesAsync();
        var query = new PaginationParams { PageNumber = 1, PageSize = 2, Search = "Sci" };

        // Act
        var result = await _repository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Categories.GetAll);
        result.Data!.TotalCount.Should().Be(1);
        result.Data.Data.Single().Name.Should().Be("Science");
    }
}
