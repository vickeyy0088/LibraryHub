using FluentAssertions;
using LibraryHub.Data;
using LibraryHub.Models;
using LibraryHub.Services;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibraryHub.Tests.UnitTests.MemberServiceTests;

public class MemberRepository_DeleteAsyncTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MemberRepository _repository;

    public MemberRepository_DeleteAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new MemberRepository(_dbContext, new Mock<ILogger<MemberRepository>>().Object);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenIdIsInvalidOrMemberDoesNotExist()
    {
        // Act
        var invalidResult = await _repository.DeleteAsync(0);
        var missingResult = await _repository.DeleteAsync(999);

        // Assert
        invalidResult.Message.Should().Be(ApiMessage.Common.InvalidId);
        missingResult.Message.Should().Be(ApiMessage.Members.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_WhenMemberHasNoLoans()
    {
        // Arrange
        await _dbContext.Users.AddAsync(new ApplicationUser { Id = "user-1", Email = "reader@test.com", FullName = "Test Reader" });
        await _dbContext.Members.AddAsync(new Member { Id = 1, ApplicationUserId = "user-1", MembershipNumber = "M-1" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(ApiMessage.Members.Deleted);
        var deleted = await _dbContext.Members.IgnoreQueryFilters().SingleAsync(m => m.Id == 1);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
    }
}
