
using LibraryHub.Data;
using LibraryHub.DTOs.Pagination;
using LibraryHub.DTOs.Report;
using LibraryHub.Extensions;
using LibraryHub.Interfaces;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Services;

public class ReportRepository(
    ApplicationDbContext context,
    ILogger<ReportRepository> logger) : IReportRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<ReportRepository> _logger = logger;

    public async Task<ApiResponse<PaginationResponseDto<MemberActivityDto>>> GetMemberActivityAsync(PaginationParams page)
    {
        if (page.Limit < 1)
            return InvalidLimit<MemberActivityDto>();

        if (page.PageNumber < 1 || page.PageSize < 1)
            return InvalidPagination<MemberActivityDto>();

        var limit = Math.Min(page.Limit, 100);
        var today = DateTime.UtcNow;

        _logger.LogInformation(
            "Generating member activity report. Page: {PageNumber}, PageSize: {PageSize}, Limit: {Limit}, Search: {Search}",
            page.PageNumber, page.PageSize, limit, page.Search);

        var query = _context.Members
            .AsNoTracking()
            .WhereSearch(page.Search, m => m.MembershipNumber)
            .Select(m => new MemberActivityDto
            {
                MemberId = m.Id,
                MembershipNumber = m.MembershipNumber,
                LoanCount = m.Loans.Count(),
                OverdueCount = m.Loans.Count(l =>
                    l.ReturnDate == null && l.DueDate < today)
            })
            .OrderByDescending(m => m.LoanCount)
            .ThenBy(m => m.MembershipNumber)
            .Take(limit);

        var result = await query.ToPaginatedResponseAsync(
            page.PageNumber, page.PageSize);

        _logger.LogInformation(
            "Member activity report generated successfully.");

        return ApiResponse<PaginationResponseDto<MemberActivityDto>>
            .Ok(ApiMessage.Reports.MemberActivity, result);
    }

    public async Task<ApiResponse<PaginationResponseDto<PopularBookDto>>> GetPopularBooksAsync(PaginationParams page)
    {
        if (page.Limit < 1)
            return InvalidLimit<PopularBookDto>();

        if (page.PageNumber < 1 || page.PageSize < 1)
            return InvalidPagination<PopularBookDto>();

        var limit = Math.Min(page.Limit, 100);

        _logger.LogInformation(
            "Generating popular books report. Page: {PageNumber}, PageSize: {PageSize}, Limit: {Limit}, Search: {Search}",
            page.PageNumber, page.PageSize, limit, page.Search);

        var query = _context.Books
            .AsNoTracking()
            .WhereSearch(page.Search, b => b.Title, b => b.ISBN)
            .Select(b => new PopularBookDto
            {
                BookId = b.Id,
                Title = b.Title,
                ISBN = b.ISBN,
                TotalCopies = b.TotalCopies,
                BorrowCount = b.Loans.Count()
            })
            .OrderByDescending(b => b.BorrowCount)
            .ThenBy(b => b.Title)
            .Take(limit);

        var result = await query.ToPaginatedResponseAsync(
            page.PageNumber, page.PageSize);

        _logger.LogInformation(
            "Popular books report generated successfully.");

        return ApiResponse<PaginationResponseDto<PopularBookDto>>
            .Ok(ApiMessage.Reports.PopularBooks, result);
    }

    private ApiResponse<PaginationResponseDto<T>> InvalidLimit<T>()
    {
        _logger.LogWarning("Report request failed: Invalid limit.");

        return ApiResponse<PaginationResponseDto<T>>
            .Fail(ApiMessage.Reports.InvalidLimit);
    }

    private ApiResponse<PaginationResponseDto<T>> InvalidPagination<T>()
    {
        _logger.LogWarning("Report request failed: Invalid pagination parameters.");

        return ApiResponse<PaginationResponseDto<T>>
            .Fail(ApiMessage.Common.InvalidPaginationParameters);
    }
}
