using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.MemberServiceTests;

public class MemberRepository_GetAllAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MemberRepository _repository;

    public MemberRepository_GetAllAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new MemberRepository(_dbContext, new Mock<ILogger<MemberRepository>>().Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFail_WhenPaginationIsInvalid()
    {
        // Arrange
        var query = new PaginationParams { PageNumber = 0, PageSize = 10 };

        // Act
        var result = await _repository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidPaginationParameters);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", Email = "reader@test.com", FullName = "Test Reader" });
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-2", Email = "other@test.com", FullName = "Other Reader" });
        await _dbContext.Members.AddRangeAsync(
            new Member { Id = 1, ApplicationUserId = "user-1", MembershipNumber = "M-1" },
            new Member { Id = 2, ApplicationUserId = "user-2", MembershipNumber = "M-2" });
        await _dbContext.SaveChangesAsync();
        var query = new PaginationParams { PageNumber = 1, PageSize = 10, Search = "Test Reader" };

        // Act
        var result = await _repository.GetAllAsync(query);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Members.GetAll);
        result.Data!.TotalCount.Should().Be(1);
        result.Data.Data.Single().MembershipNumber.Should().Be("M-1");
    }
}
