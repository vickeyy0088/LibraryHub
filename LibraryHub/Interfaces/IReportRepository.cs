using LibraryHub.DTOs.Pagination;
using LibraryHub.DTOs.Report;
using LibraryHub.Shared;

namespace LibraryHub.Interfaces;

public interface IReportRepository
{
    Task<ApiResponse<PaginationResponseDto<PopularBookDto>>> GetPopularBooksAsync(PaginationParams query);
    Task<ApiResponse<PaginationResponseDto<MemberActivityDto>>> GetMemberActivityAsync(PaginationParams query);
}
