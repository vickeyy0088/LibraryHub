
using LibraryHub.Data;
using LibraryHub.DTOs.Category;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Extensions;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Services;

public class CategoryRepository(
    ApplicationDbContext context,
    ILogger<CategoryRepository> logger) : ICategoryRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<CategoryRepository> _logger = logger;

    public async Task<ApiResponse<PaginationResponseDto<CategoryResponseDto>>> GetAllAsync(
        PaginationParams query)
    {
        if (query.PageNumber < 1 || query.PageSize < 1)
        {
            _logger.LogWarning("Invalid category pagination: PageNumber={PageNumber}, PageSize={PageSize}",
                query.PageNumber, query.PageSize);

            return ApiResponse<PaginationResponseDto<CategoryResponseDto>>
                .Fail(ApiMessage.Common.InvalidPaginationParameters);
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .WhereSearch(query.Search, c => c.Name)
            .OrderBy(c => c.Id)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .ToPaginatedResponseAsync(query.PageNumber, query.PageSize);

        _logger.LogInformation("Retrieved categories. PageNumber={PageNumber}, PageSize={PageSize}",
            query.PageNumber, query.PageSize);

        return ApiResponse<PaginationResponseDto<CategoryResponseDto>>
            .Ok(ApiMessage.Categories.GetAll, categories);
    }

    public async Task<ApiResponse<CategoryResponseDto?>> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid category ID: {CategoryId}", id);
            return ApiResponse<CategoryResponseDto?>.Fail(ApiMessage.Common.InvalidId);
        }

        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .FirstOrDefaultAsync();

        if (category == null)
        {
            _logger.LogWarning("Category not found: {CategoryId}", id);
            return ApiResponse<CategoryResponseDto?>.Fail(ApiMessage.Categories.NotFound);
        }

        _logger.LogInformation("Retrieved category: {CategoryId}", id);

        return ApiResponse<CategoryResponseDto?>.Ok(
            ApiMessage.Categories.GetById, category);
    }

    public async Task<ApiResponse<CategoryResponseDto>> CreateAsync(CategoryCreateDto request)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Category creation failed: Name is required.");
            return ApiResponse<CategoryResponseDto>.Fail(ApiMessage.Categories.NameIsRequired);
        }

        if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
        {
            _logger.LogWarning("Category creation failed: Category already exists. Name={Name}", name);
            return ApiResponse<CategoryResponseDto>.Fail(ApiMessage.Categories.AlreadyExists);
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category created: {CategoryId} - {Name}",
            category.Id, category.Name);

        return ApiResponse<CategoryResponseDto>.Ok(ApiMessage.Categories.Created,
            new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt
            });
    }

    public async Task<ApiResponse<CategoryResponseDto>> UpdateAsync(
        int id, CategoryUpdateDto request)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid category ID for update: {CategoryId}", id);
            return ApiResponse<CategoryResponseDto>.Fail(ApiMessage.Common.InvalidId);
        }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            _logger.LogWarning("Category update failed: Category not found. ID={CategoryId}", id);
            return ApiResponse<CategoryResponseDto>.Fail(ApiMessage.Categories.NotFound);
        }

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Category update failed: Name is required. ID={CategoryId}", id);
            return ApiResponse<CategoryResponseDto>.Fail(ApiMessage.Categories.NameIsRequired);
        }

        if (await _context.Categories.AnyAsync(c =>
            c.Id != id && c.Name.ToLower() == name.ToLower()))
        {
            _logger.LogWarning("Category update failed: Duplicate name. ID={CategoryId}, Name={Name}",
                id, name);

            return ApiResponse<CategoryResponseDto>.Fail(ApiMessage.Categories.AlreadyExists);
        }

        category.Name = name;
        category.Description = request.Description?.Trim();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Category updated: {CategoryId} - {Name}",
            category.Id, category.Name);

        return ApiResponse<CategoryResponseDto>.Ok(ApiMessage.Categories.Updated,
            new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt
            });
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid category ID for deletion: {CategoryId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Common.InvalidId);
        }

        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            _logger.LogWarning("Category deletion failed: Category not found. ID={CategoryId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Categories.NotFound);
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category deleted: {CategoryId} - {Name}",
            category.Id, category.Name);

        return ApiResponse<bool>.Ok(ApiMessage.Categories.Deleted, true);
    }
}