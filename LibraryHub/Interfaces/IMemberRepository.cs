using LibraryHub.DTOs.Member;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Shared;

namespace LibraryHub.Interfaces;

public interface IMemberRepository
{
    Task<ApiResponse<PaginationResponseDto<MemberResponseDto>>> GetAllAsync(PaginationParams query);
    Task<ApiResponse<MemberResponseDto?>> GetByIdAsync(int id);
    Task<ApiResponse<MemberResponseDto>> CreateAsync(MemberCreateDto request);
    Task<ApiResponse<MemberResponseDto>> UpdateAsync(int id, MemberUpdateDto request);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
