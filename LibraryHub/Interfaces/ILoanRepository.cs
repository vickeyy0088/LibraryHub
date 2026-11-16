using LibraryHub.DTOs.Loan;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Shared;

namespace LibraryHub.Interfaces;

public interface ILoanRepository
{
    Task<ApiResponse<PaginationResponseDto<LoanResponseDto>>> GetAllAsync(string? userId, bool isStaff, PaginationParams query);
    Task<ApiResponse<LoanResponseDto?>> CreateAsync(LoanCreateDto request);
    Task<ApiResponse<bool>> ReturnAsync(int id);
    Task<ApiResponse<IEnumerable<LoanResponseDto>>> GetOverdueAsync();
}
