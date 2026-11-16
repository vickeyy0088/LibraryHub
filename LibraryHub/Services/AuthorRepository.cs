
using LibraryHub.Data;
using LibraryHub.DTOs.Author;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Extensions;
using LibraryHub.Helpers;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Services;

public class AuthorRepository(
    ApplicationDbContext context,
    ILogger<AuthorRepository> logger,
    ICacheHelper cache) : IAuthorRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<AuthorRepository> _logger = logger;
    private readonly ICacheHelper _cache = cache;

    private const string AuthorByIdPrefix = "authors:id:";

    public async Task<ApiResponse<PaginationResponseDto<AuthorResponseDto>>> GetAllAsync(
     PaginationParams query)
    {
        if (query.PageNumber < 1 || query.PageSize < 1)
        {
            _logger.LogWarning("Invalid author pagination parameters.");

            return ApiResponse<PaginationResponseDto<AuthorResponseDto>>.Fail(
                ApiMessage.Common.InvalidPaginationParameters);
        }

        string search = query.Search?
            .Trim()
            .ToLowerInvariant() ?? string.Empty;

        string cacheKey =
            $"authors:p:{query.PageNumber}:s:{query.PageSize}:q:{search}";

        var authors = await _cache.GetOrSetAsync(
            cacheKey,
            async cancellationToken =>
            {
                _logger.LogInformation(
                    "Authors not found in cache. Loading from database. Page: {PageNumber}",
                    query.PageNumber);

                var result = _context.Authors
                    .AsNoTracking()
                    .WhereSearch(
                        query.Search,
                        a => a.FirstName,
                        a => a.LastName)
                    .OrderBy(a => a.Id)
                    .Select(a => new AuthorResponseDto
                    {
                        Id = a.Id,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        Biography = a.Biography
                    });

                return await result.ToPaginatedResponseAsync(
                    query.PageNumber,
                    query.PageSize);
            },
            absoluteExpiration: TimeSpan.FromMinutes(1));

        return ApiResponse<PaginationResponseDto<AuthorResponseDto>>.Ok(
            ApiMessage.Authors.GetAll,
            authors);
    }

    public async Task<ApiResponse<AuthorResponseDto?>> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning(
                "Invalid author ID: {AuthorId}",
                id);

            return ApiResponse<AuthorResponseDto?>.Fail(
                ApiMessage.Common.InvalidId);
        }

        string cacheKey = $"{AuthorByIdPrefix}{id}";

        var author = await _cache.GetOrSetAsync(
            cacheKey,
            async cancellationToken =>
            {
                _logger.LogInformation(
                    "Author not found in cache. Loading from database. AuthorId: {AuthorId}",
                    id);

                return await _context.Authors
                    .AsNoTracking()
                    .Where(a => a.Id == id)
                    .Select(a => new AuthorResponseDto
                    {
                        Id = a.Id,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        Biography = a.Biography
                    })
                    .FirstOrDefaultAsync();
            },
            absoluteExpiration: TimeSpan.FromMinutes(1),
            slidingExpiration: TimeSpan.FromMinutes(1));

        if (author is null)
        {
            _logger.LogWarning(
                "Author not found. AuthorId: {AuthorId}",
                id);

            return ApiResponse<AuthorResponseDto?>.Fail(
                ApiMessage.Authors.NotFound);
        }

        return ApiResponse<AuthorResponseDto?>.Ok(
            ApiMessage.Authors.GetById,
            author);
    }

    public async Task<ApiResponse<AuthorResponseDto>> CreateAsync(
        AuthorCreateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            _logger.LogWarning("Author creation failed. FirstName or LastName is missing.");
            return ApiResponse<AuthorResponseDto>.Fail(
                ApiMessage.Authors.FullNameIsRequired);
        }

        var author = new Author
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Biography = request.Biography
        };

        await _context.Authors.AddAsync(author);
        await _context.SaveChangesAsync();

        var responseDto = new AuthorResponseDto
        {
            Id = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            Biography = author.Biography
        };

        return ApiResponse<AuthorResponseDto>.Ok(
            ApiMessage.Authors.Created, responseDto);
    }

    public async Task<ApiResponse<AuthorResponseDto>> UpdateAsync(
        int id, AuthorUpdateDto request)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid author ID for update: {AuthorId}", id);
            return ApiResponse<AuthorResponseDto>.Fail(ApiMessage.Common.InvalidId);
        }

        var author = await _context.Authors.FindAsync(id);

        if (author == null)
        {
            _logger.LogWarning("Author not found for update. AuthorId: {AuthorId}", id);
            return ApiResponse<AuthorResponseDto>.Fail(ApiMessage.Authors.NotFound);
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            _logger.LogWarning("Author update failed. Name is required. AuthorId: {AuthorId}", id);
            return ApiResponse<AuthorResponseDto>.Fail(
                ApiMessage.Authors.FullNameIsRequired);
        }

        author.FirstName = request.FirstName;
        author.LastName = request.LastName;
        author.Biography = request.Biography;

        await _context.SaveChangesAsync();

        var responseDto = new AuthorResponseDto
        {
            Id = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            Biography = author.Biography
        };

        // Overwrites the single-item cache (SetAsync)
        // await _cache.SetAsync($"{AuthorByIdPrefix}{id}", responseDto, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(5));

        // Remove the old individual cache entry
        await _cache.RemoveAsync($"{AuthorByIdPrefix}{id}");

        // Invalidate all cached author list page
        return ApiResponse<AuthorResponseDto>.Ok(
            ApiMessage.Authors.Updated, responseDto);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid author ID for deletion: {AuthorId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Common.InvalidId);
        }

        var author = await _context.Authors.FindAsync(id);

        if (author == null)
        {
            _logger.LogWarning("Author not found for deletion. AuthorId: {AuthorId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Authors.NotFound);
        }

        _context.Authors.Remove(author);
        await _context.SaveChangesAsync();

        // Deletes the single-item cache
        await _cache.RemoveAsync($"{AuthorByIdPrefix}{id}");
        return ApiResponse<bool>.Ok(ApiMessage.Authors.Deleted, true);
    }
}
