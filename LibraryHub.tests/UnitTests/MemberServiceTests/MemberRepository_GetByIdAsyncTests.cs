using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.MemberServiceTests;

public class MemberRepository_GetByIdAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MemberRepository _repository;

    public MemberRepository_GetByIdAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new MemberRepository(_dbContext, new Mock<ILogger<MemberRepository>>().Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFail_WhenIdIsInvalid()
    {
        // Act
        var result = await _repository.GetByIdAsync(0);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Common.InvalidId);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldSucceed_WhenMemberExists()
    {
        // Arrange
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", Email = "reader@test.com", FullName = "Test Reader" });
        await _dbContext.Members.AddAsync(new Member { Id = 1, ApplicationUserId = "user-1", MembershipNumber = "M-1" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Members.GetById);
        result.Data!.MembershipNumber.Should().Be("M-1");
        result.Data.FullName.Should().Be("Test Reader");
    }
}
