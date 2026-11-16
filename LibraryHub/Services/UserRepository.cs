using LibraryHub.DTOs.Models;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Extensions;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Services;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    public UserRepository(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ApiResponse<bool>> AssignRoleAsync(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ApiResponse<bool>.Fail(ApiMessage.Common.InvalidId);

        if (string.IsNullOrWhiteSpace(role))
            return ApiResponse<bool>.Fail(ApiMessage.Users.RoleNotFound);

        role = role.Trim();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return ApiResponse<bool>.Fail(ApiMessage.Users.NotFound);

        if (!await _roleManager.RoleExistsAsync(role))
            return ApiResponse<bool>.Fail(ApiMessage.Users.RoleNotFound);

        if (await _userManager.IsInRoleAsync(user, role))
            return ApiResponse<bool>.Ok(
                ApiMessage.Users.AlreadyHasRole, true);

        var result = await _userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ", result.Errors.Select(e => e.Description));

            return ApiResponse<bool>.Fail(
                $"Failed to assign role: {errors}");
        }

        return ApiResponse<bool>.Ok(
            ApiMessage.Users.RoleAssigned, true);
    }


    public async Task<ApiResponse<PaginationResponseDto<UserResponseDto>>> GetAllAsync(PaginationParams page)
    {
        if (page.PageNumber < 1 || page.PageSize < 1)
            return ApiResponse<PaginationResponseDto<UserResponseDto>>
                .Fail(ApiMessage.Common.InvalidPaginationParameters);

        var pageSize = Math.Min(page.PageSize, 100);

        var query = _userManager.Users
            .AsNoTracking()
            .WhereSearch(page.Search, u => u.UserName, u => u.Email, u => u.FullName)
            .OrderBy(u => u.Id)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName
            });

        var paginatedResult = await query.ToPaginatedResponseAsync(page.PageNumber, pageSize);

        var userIds = paginatedResult.Data
            .Select(u => u.Id)
            .ToList();

        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        foreach (var userDto in paginatedResult.Data)
        {
            if (users.TryGetValue(userDto.Id, out var user))
                userDto.Roles = await _userManager.GetRolesAsync(user);
        }

        return ApiResponse<PaginationResponseDto<UserResponseDto>>
            .Ok(ApiMessage.Users.GetAll, paginatedResult);
    }

    public async Task<ApiResponse<bool>> RemoveRoleAsync(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return ApiResponse<bool>.Fail(ApiMessage.Common.InvalidId);

        if (string.IsNullOrWhiteSpace(role))
            return ApiResponse<bool>.Fail(ApiMessage.Users.RoleNotFound);

        role = role.Trim();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return ApiResponse<bool>.Fail(ApiMessage.Users.NotFound);

        if (!await _roleManager.RoleExistsAsync(role))
            return ApiResponse<bool>.Fail(ApiMessage.Users.RoleNotFound);

        if (!await _userManager.IsInRoleAsync(user, role))
            return ApiResponse<bool>.Ok(
                ApiMessage.Users.AlreadyHasRole, true);

        var result = await _userManager.RemoveFromRoleAsync(user, role);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ", result.Errors.Select(e => e.Description));
            return ApiResponse<bool>.Fail(
                $"Failed to remove role: {errors}");
        }

        return ApiResponse<bool>.Ok(
            ApiMessage.Users.RoleRemoved, true);
    }
}
