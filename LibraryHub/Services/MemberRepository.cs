
using LibraryHub.Data;
using LibraryHub.DTOs.Member;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Extensions;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Services;

public class MemberRepository(
    ApplicationDbContext context,
    ILogger<MemberRepository> logger) : IMemberRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<MemberRepository> _logger = logger;

    public async Task<ApiResponse<PaginationResponseDto<MemberResponseDto>>> GetAllAsync(
        PaginationParams query)
    {
        if (query.PageNumber < 1 || query.PageSize < 1)
        {
            _logger.LogWarning("Invalid member pagination: PageNumber={PageNumber}, PageSize={PageSize}",
                query.PageNumber, query.PageSize);

            return ApiResponse<PaginationResponseDto<MemberResponseDto>>
                .Fail(ApiMessage.Common.InvalidPaginationParameters);
        }

        var members = await _context.Members
            .AsNoTracking()
            .WhereSearch(query.Search,
                m => m.MembershipNumber,
                m => m.ApplicationUser.FullName,
                m => m.ApplicationUser.Email)
            .OrderBy(m => m.Id)
            .Select(m => new MemberResponseDto
            {
                Id = m.Id,
                MembershipNumber = m.MembershipNumber,
                ApplicationUserId = m.ApplicationUserId,
                FullName = m.ApplicationUser.FullName,
                Email = m.ApplicationUser.Email,
                MembershipDate = m.MembershipDate,
                IsActive = m.IsActive
            })
            .ToPaginatedResponseAsync(query.PageNumber, query.PageSize);

        _logger.LogInformation("Retrieved members. PageNumber={PageNumber}, PageSize={PageSize}",
            query.PageNumber, query.PageSize);

        return ApiResponse<PaginationResponseDto<MemberResponseDto>>
            .Ok(ApiMessage.Members.GetAll, members);
    }

    public async Task<ApiResponse<MemberResponseDto?>> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid member ID: {MemberId}", id);
            return ApiResponse<MemberResponseDto?>.Fail(ApiMessage.Common.InvalidId);
        }

        var member = await _context.Members
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MemberResponseDto
            {
                Id = m.Id,
                MembershipNumber = m.MembershipNumber,
                ApplicationUserId = m.ApplicationUserId,
                FullName = m.ApplicationUser.FullName,
                Email = m.ApplicationUser.Email,
                MembershipDate = m.MembershipDate,
                IsActive = m.IsActive
            })
            .FirstOrDefaultAsync();

        if (member == null)
        {
            _logger.LogWarning("Member not found: {MemberId}", id);
            return ApiResponse<MemberResponseDto?>.Fail(ApiMessage.Members.NotFound);
        }

        _logger.LogInformation("Retrieved member: {MemberId}", id);

        return ApiResponse<MemberResponseDto?>.Ok(ApiMessage.Members.GetById, member);
    }

    public async Task<ApiResponse<MemberResponseDto>> CreateAsync(MemberCreateDto request)
    {
        var userId = request.ApplicationUserId?.Trim();
        var membershipNumber = request.MembershipNumber?.Trim();

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Member creation failed: ApplicationUserId is required.");
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Members.InvalidApplicationUserId);
        }

        if (string.IsNullOrWhiteSpace(membershipNumber))
        {
            _logger.LogWarning("Member creation failed: MembershipNumber is required.");
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Members.InvalidMembershipNumber);
        }

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            _logger.LogWarning("Member creation failed: User not found. ApplicationUserId={UserId}", userId);
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Members.InvalidApplicationUserId);
        }

        if (await _context.Members.AnyAsync(m => m.ApplicationUserId == userId))
        {
            _logger.LogWarning("Member creation failed: User is already a member. ApplicationUserId={UserId}", userId);
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Members.AlreadyMember);
        }

        if (await _context.Members.AnyAsync(m => m.MembershipNumber == membershipNumber))
        {
            _logger.LogWarning("Member creation failed: Membership number already exists.");
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Members.MembershipNumberExists);
        }

        var member = new Member
        {
            ApplicationUserId = userId,
            MembershipNumber = $"MEM-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            MembershipDate = request.MembershipDate ?? DateTime.UtcNow,
            IsActive = request.IsActive
        };

        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Member created: {MemberId}, MembershipNumber={MembershipNumber}",
            member.Id, member.MembershipNumber);

        return ApiResponse<MemberResponseDto>.Ok(ApiMessage.Members.Created,
            new MemberResponseDto
            {
                Id = member.Id,
                MembershipNumber = member.MembershipNumber,
                ApplicationUserId = member.ApplicationUserId,
                FullName = user.FullName,
                Email = user.Email,
                MembershipDate = member.MembershipDate,
                IsActive = member.IsActive
            });
    }

    public async Task<ApiResponse<MemberResponseDto>> UpdateAsync(
        int id, MemberUpdateDto request)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid member ID for update: {MemberId}", id);
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Common.InvalidId);
        }

        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == id);

        if (member == null)
        {
            _logger.LogWarning("Member update failed: Member not found. MemberId={MemberId}", id);
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Members.NotFound);
        }

        var membershipNumber = request.MembershipNumber?.Trim();

        if (string.IsNullOrWhiteSpace(membershipNumber))
        {
            _logger.LogWarning("Member update failed: Membership number is required. MemberId={MemberId}", id);
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Members.InvalidMembershipNumber);
        }

        if (await _context.Members.AnyAsync(m =>
            m.Id != id && m.MembershipNumber == membershipNumber))
        {
            _logger.LogWarning("Member update failed: Duplicate membership number. MemberId={MemberId}", id);
            return ApiResponse<MemberResponseDto>.Fail(ApiMessage.Members.MembershipNumberExists);
        }

        member.MembershipNumber = membershipNumber;
        member.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        var user = await _context.Users
            .Where(u => u.Id == member.ApplicationUserId)
            .Select(u => new { u.FullName, u.Email })
            .FirstAsync();

        _logger.LogInformation("Member updated: {MemberId}, MembershipNumber={MembershipNumber}",
            member.Id, member.MembershipNumber);

        return ApiResponse<MemberResponseDto>.Ok(ApiMessage.Members.Updated,
            new MemberResponseDto
            {
                Id = member.Id,
                MembershipNumber = member.MembershipNumber,
                ApplicationUserId = member.ApplicationUserId,
                FullName = user.FullName,
                Email = user.Email,
                MembershipDate = member.MembershipDate,
                IsActive = member.IsActive
            });
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid member ID for deletion: {MemberId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Common.InvalidId);
        }

        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == id);

        if (member == null)
        {
            _logger.LogWarning("Member deletion failed: Member not found. MemberId={MemberId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Members.NotFound);
        }

        if (await _context.Loans.AnyAsync(l => l.MemberId == id))
        {
            _logger.LogWarning("Member deletion failed: Member has existing loans. MemberId={MemberId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Members.HasLoans);
        }

        _context.Members.Remove(member);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Member deleted: {MemberId}, MembershipNumber={MembershipNumber}",
            member.Id, member.MembershipNumber);

        return ApiResponse<bool>.Ok(ApiMessage.Members.Deleted, true);
    }
}
