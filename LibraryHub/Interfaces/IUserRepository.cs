using LibraryHub.DTOs.Models;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Shared;

namespace LibraryHub.Interfaces;

public interface IUserRepository
{
    Task<ApiResponse<PaginationResponseDto<UserResponseDto>>> GetAllAsync(PaginationParams query);
    Task<ApiResponse<bool>> AssignRoleAsync(string userId, string role);
    Task<ApiResponse<bool>> RemoveRoleAsync(string userId, string role);
}
