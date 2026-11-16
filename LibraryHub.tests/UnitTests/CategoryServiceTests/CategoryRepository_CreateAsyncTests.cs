using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Category;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.CategoryServiceTests;

public class CategoryRepository_CreateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CategoryRepository _repository;

    public CategoryRepository_CreateAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new CategoryRepository(_dbContext, new Mock<ILogger<CategoryRepository>>().Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ShouldFail_WhenNameIsMissing(string? name)
    {
        // Arrange
        var request = new CategoryCreateDto { Name = name! };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Categories.NameIsRequired);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenRequestIsValidAndTrimValues()
    {
        // Arrange
        var request = new CategoryCreateDto { Name = "  Fiction ", Description = "  Books  " };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Categories.Created);
        result.Data!.Name.Should().Be("Fiction");
        result.Data.Description.Should().Be("Books");
        (await _dbContext.Categories.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenNameAlreadyExistsIgnoringCase()
    {
        // Arrange
        await _dbContext.Categories.AddAsync(new Models.Category { Name = "Fiction" });
        await _dbContext.SaveChangesAsync();
        // Act
        var result = await _repository.CreateAsync(new CategoryCreateDto { Name = " fiction " });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Categories.AlreadyExists);
    }
}
