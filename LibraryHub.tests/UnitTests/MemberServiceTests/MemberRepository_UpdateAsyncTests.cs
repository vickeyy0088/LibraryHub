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

public class MemberRepository_UpdateAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MemberRepository _repository;

    public MemberRepository_UpdateAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new MemberRepository(_dbContext, new Mock<ILogger<MemberRepository>>().Object);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenIdIsInvalidOrMemberDoesNotExist()
    {
        // Arrange
        var request = new MemberUpdateDto { MembershipNumber = "M-UPDATED" };

        // Act
        var invalidResult = await _repository.UpdateAsync(0, request);
        var missingResult = await _repository.UpdateAsync(999, request);

        // Assert
        invalidResult.Message.Should().Be(ApiMessage.Common.InvalidId);
        missingResult.Message.Should().Be(ApiMessage.Members.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", Email = "reader@test.com", FullName = "Test Reader" });
        await _dbContext.Members.AddAsync(new Member { Id = 1, ApplicationUserId = "user-1", MembershipNumber = "M-1" });
        await _dbContext.SaveChangesAsync();
        var request = new MemberUpdateDto { MembershipNumber = "M-UPDATED", IsActive = false };

        // Act
        var result = await _repository.UpdateAsync(1, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Members.Updated);
        result.Data!.MembershipNumber.Should().Be("M-UPDATED");
        result.Data.IsActive.Should().BeFalse();
    }
}
