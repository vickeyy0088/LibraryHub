using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.CategoryServiceTests;

public class CategoryRepository_GetByIdAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CategoryRepository _repository;

    public CategoryRepository_GetByIdAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new CategoryRepository(_dbContext, new Mock<ILogger<CategoryRepository>>().Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_ShouldFail_WhenIdIsInvalid(int id)
    {
        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFail_WhenCategoryDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Categories.NotFound);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldSucceed_WhenCategoryExists()
    {
        // Arrange
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Fiction", Description = "Books" });
        await _dbContext.SaveChangesAsync();
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Categories.GetById);
        result.Data!.Name.Should().Be("Fiction");
        result.Data.Description.Should().Be("Books");
    }
}
