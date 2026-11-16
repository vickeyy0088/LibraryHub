using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.CategoryServiceTests;

public class CategoryRepository_DeleteAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CategoryRepository _repository;

    public CategoryRepository_DeleteAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new CategoryRepository(_dbContext, new Mock<ILogger<CategoryRepository>>().Object);
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
    public async Task DeleteAsync_ShouldFail_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _repository.DeleteAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Categories.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_WhenCategoryExists()
    {
        // Arrange
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction" });
        await _dbContext.SaveChangesAsync();
        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Categories.Deleted);
        result.Data.Should().BeTrue();
        var deleted = await _dbContext.Categories.IgnoreQueryFilters().SingleAsync(c => c.Id == 1);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
    }
}
