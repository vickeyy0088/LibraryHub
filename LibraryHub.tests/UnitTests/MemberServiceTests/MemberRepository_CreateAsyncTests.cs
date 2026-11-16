using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.DTOs.Member;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.MemberServiceTests;

public class MemberRepository_CreateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MemberRepository _repository;

    public MemberRepository_CreateAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new MemberRepository(_dbContext, new Mock<ILogger<MemberRepository>>().Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new MemberCreateDto { ApplicationUserId = "missing", MembershipNumber = "M-1" };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ApiMessage.Members.InvalidApplicationUserId);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenUserExists()
    {
        // Arrange
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", Email = "reader@test.com", FullName = "Test Reader" });
        await _dbContext.SaveChangesAsync();
        var request = new MemberCreateDto { ApplicationUserId = "user-1", MembershipNumber = "M-1", IsActive = true };

        // Act
        var result = await _repository.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Members.Created);
        result.Data!.ApplicationUserId.Should().Be("user-1");
        result.Data.MembershipNumber.Should().StartWith("MEM-");
        (await _dbContext.Members.CountAsync()).Should().Be(1);
    }
}
