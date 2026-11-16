using LibraryHub.DTOs.Author;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Shared;

namespace LibraryHub.Interfaces;

public interface IAuthorRepository
{
    Task<ApiResponse<PaginationResponseDto<AuthorResponseDto>>> GetAllAsync(PaginationParams query);
    Task<ApiResponse<AuthorResponseDto?>> GetByIdAsync(int id);
    Task<ApiResponse<AuthorResponseDto>> CreateAsync(AuthorCreateDto author);
    Task<ApiResponse<AuthorResponseDto>> UpdateAsync(int id, AuthorUpdateDto author);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
