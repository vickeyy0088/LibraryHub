using LibraryHub.DTOs.Category;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Shared;

namespace LibraryHub.Interfaces;

public interface ICategoryRepository
{
    Task<ApiResponse<PaginationResponseDto<CategoryResponseDto>>> GetAllAsync(PaginationParams query);
    Task<ApiResponse<CategoryResponseDto?>> GetByIdAsync(int id);
    Task<ApiResponse<CategoryResponseDto>> CreateAsync(CategoryCreateDto category);
    Task<ApiResponse<CategoryResponseDto>> UpdateAsync(int id, CategoryUpdateDto category);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
