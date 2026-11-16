using LibraryHub.DTOs.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Extensions;

public static class PaginationExtensions
{
    public static async Task<PaginationResponseDto<T>> ToPaginatedResponseAsync<T>(
                                  this IQueryable<T> query,
                                  int pageNumber = 1,
                                  int pageSize = 10)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        pageSize = Math.Min(pageSize, 100);

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginationResponseDto<T>
        {
            Data = data,
            TotalCount = totalCount
        };
    }
}
