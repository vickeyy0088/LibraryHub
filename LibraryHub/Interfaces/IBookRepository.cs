using LibraryHub.DTOs.Book;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Shared;

namespace LibraryHub.Interfaces;

public interface IBookRepository
{
    Task<ApiResponse<PaginationResponseDto<BookResponseDto>>> GetAllAsync(PaginationParams query);
    Task<ApiResponse<BookResponseDto?>> GetByIdAsync(int id);
    Task<ApiResponse<BookResponseDto>> CreateAsync(BookCreateDto book);
    Task<ApiResponse<BookResponseDto>> UpdateAsync(int id, BookUpdateDto book);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
