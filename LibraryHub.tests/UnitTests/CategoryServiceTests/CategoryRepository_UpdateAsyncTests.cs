using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Category;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.CategoryServiceTests;

public class CategoryRepository_UpdateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CategoryRepository _repository;

    public CategoryRepository_UpdateAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new CategoryRepository(_dbContext, new Mock<ILogger<CategoryRepository>>().Object);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenIdIsInvalidOrCategoryDoesNotExist()
    {
        // Arrange
        var request = new CategoryUpdateDto { Name = "Updated" };

        // Act
        var invalidIdResult = await _repository.UpdateAsync(0, request);
        var missingResult = await _repository.UpdateAsync(999, request);

        // Assert
        invalidIdResult.Message.Should().Be(ApiMessage.Common.InvalidId);
        missingResult.Message.Should().Be(ApiMessage.Categories.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenNameIsMissing()
    {
        // Arrange
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Original" });
        await _dbContext.SaveChangesAsync();
        var request = new CategoryUpdateDto { Name = " " };

        // Act
        var result = await _repository.UpdateAsync(1, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Categories.NameIsRequired);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenNameAlreadyExists()
    {
        // Arrange
        await _dbContext.Categories.AddRangeAsync(new Category { Id = 1, Name = "Original" }, new Category { Id = 2, Name = "Other" });
        await _dbContext.SaveChangesAsync();
        var request = new CategoryUpdateDto { Name = " other " };

        // Act
        var result = await _repository.UpdateAsync(1, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Categories.AlreadyExists);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        await _dbContext.Categories.AddAsync(new Category { Id = 1, Name = "Original" });
        await _dbContext.SaveChangesAsync();
        var request = new CategoryUpdateDto { Name = " Updated ", Description = " New description " };

        // Act
        var result = await _repository.UpdateAsync(1, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Categories.Updated);
        result.Data!.Name.Should().Be("Updated");
        result.Data.Description.Should().Be("New description");
        (await _dbContext.Categories.FindAsync(1))!.Name.Should().Be("Updated");
    }
}
