
using LibraryHub.Data;
using LibraryHub.DTOs.Loan;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Extensions;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Services;

public class LoanRepository(
    ApplicationDbContext context,
    ILogger<LoanRepository> logger) : ILoanRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<LoanRepository> _logger = logger;

    private static LoanResponseDto ToDto(Loan loan) => new()
    {
        Id = loan.Id,
        BookId = loan.BookId,
        MemberId = loan.MemberId,
        LoanDate = loan.LoanDate,
        DueDate = loan.DueDate,
        ReturnDate = loan.ReturnDate,
        Status = loan.Status
    };

    public async Task<ApiResponse<PaginationResponseDto<LoanResponseDto>>> GetAllAsync(
        string? userId, bool isStaff, PaginationParams page)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Loan retrieval failed: UserId is missing.");
            return ApiResponse<PaginationResponseDto<LoanResponseDto>>
                .Fail(ApiMessage.Loans.UserIdMissing);
        }

        if (page.PageNumber < 1 || page.PageSize < 1)
        {
            _logger.LogWarning("Invalid loan pagination: PageNumber={PageNumber}, PageSize={PageSize}",
                page.PageNumber, page.PageSize);

            return ApiResponse<PaginationResponseDto<LoanResponseDto>>
                .Fail(ApiMessage.Common.InvalidPaginationParameters);
        }

        var query = _context.Loans
            .AsNoTracking()
            .WhereSearch(page.Search, l => l.Status);

        if (!isStaff)
            query = query.Where(l => l.Member.ApplicationUserId == userId);

        var loans = await query
            .OrderByDescending(l => l.LoanDate)
            .Select(l => new LoanResponseDto
            {
                Id = l.Id,
                BookId = l.BookId,
                MemberId = l.MemberId,
                LoanDate = l.LoanDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                Status = l.Status
            })
            .ToPaginatedResponseAsync(page.PageNumber, page.PageSize);

        _logger.LogInformation(
            "Retrieved loans for UserId={UserId}, IsStaff={IsStaff}",
            userId, isStaff);

        return ApiResponse<PaginationResponseDto<LoanResponseDto>>
            .Ok(ApiMessage.Loans.GetAll, loans);
    }

    public async Task<ApiResponse<LoanResponseDto?>> CreateAsync(LoanCreateDto request)
    {
        if (request.BookId <= 0 || request.MemberId <= 0)
        {
            _logger.LogWarning("Loan creation failed: Invalid BookId={BookId} or MemberId={MemberId}",
                request.BookId, request.MemberId);

            return ApiResponse<LoanResponseDto?>.Fail(ApiMessage.Loans.CreateFailed);
        }

        if (request.DueDate <= DateTime.UtcNow)
        {
            _logger.LogWarning("Loan creation failed: Invalid DueDate={DueDate}", request.DueDate);
            return ApiResponse<LoanResponseDto?>.Fail(ApiMessage.Loans.DueDateError);
        }

        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == request.BookId);

        if (book == null)
        {
            _logger.LogWarning("Loan creation failed: Book not found. BookId={BookId}", request.BookId);
            return ApiResponse<LoanResponseDto?>.Fail(ApiMessage.Books.NotFound);
        }

        if (book.AvailableCopies <= 0)
        {
            _logger.LogWarning("Loan creation failed: No available copies. BookId={BookId}", book.Id);
            return ApiResponse<LoanResponseDto?>.Fail(ApiMessage.Loans.CopiesError);
        }

        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == request.MemberId);

        if (member == null)
        {
            _logger.LogWarning("Loan creation failed: Member not found. MemberId={MemberId}", request.MemberId);
            return ApiResponse<LoanResponseDto?>.Fail(ApiMessage.Members.NotFound);
        }

        if (!member.IsActive)
        {
            _logger.LogWarning("Loan creation failed: Member is inactive. MemberId={MemberId}", member.Id);
            return ApiResponse<LoanResponseDto?>.Fail(ApiMessage.Loans.CreateFailed);
        }

        var now = DateTime.UtcNow;
        var loan = new Loan
        {
            BookId = book.Id,
            MemberId = member.Id,
            LoanDate = now,
            DueDate = request.DueDate,
            Status = "Issued",
            CreatedAt = now
        };

        book.AvailableCopies--;
        _context.Loans.Add(loan);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Loan created: LoanId={LoanId}, BookId={BookId}, MemberId={MemberId}",
            loan.Id, loan.BookId, loan.MemberId);

        return ApiResponse<LoanResponseDto?>.Ok(ApiMessage.Loans.Created, ToDto(loan));
    }

    public async Task<ApiResponse<bool>> ReturnAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Loan return failed: Invalid LoanId={LoanId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Common.InvalidId);
        }

        var loan = await _context.Loans
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null)
        {
            _logger.LogWarning("Loan return failed: Loan not found. LoanId={LoanId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Loans.NotFound);
        }

        if (loan.ReturnDate != null || loan.Status == "Returned")
        {
            _logger.LogWarning("Loan already returned: LoanId={LoanId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Loans.AlreadyReturned);
        }

        if (loan.Book == null)
        {
            _logger.LogWarning("Loan return failed: Book not found. LoanId={LoanId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Books.NotFound);
        }

        loan.ReturnDate = DateTime.UtcNow;
        loan.Status = "Returned";
        loan.Book.AvailableCopies++;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Loan returned: LoanId={LoanId}, BookId={BookId}, MemberId={MemberId}",
            loan.Id, loan.BookId, loan.MemberId);

        return ApiResponse<bool>.Ok(ApiMessage.Loans.Returned, true);
    }

    public async Task<ApiResponse<IEnumerable<LoanResponseDto>>> GetOverdueAsync()
    {
        var now = DateTime.UtcNow;

        var loans = await _context.Loans
            .AsNoTracking()
            .Where(l => l.DueDate < now && l.ReturnDate == null)
            .OrderBy(l => l.DueDate)
            .Select(l => new LoanResponseDto
            {
                Id = l.Id,
                BookId = l.BookId,
                MemberId = l.MemberId,
                LoanDate = l.LoanDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                Status = l.Status
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {LoanCount} overdue loans.", loans.Count);

        return ApiResponse<IEnumerable<LoanResponseDto>>
            .Ok(ApiMessage.Loans.GetOverdueLoans, loans);
    }
}
