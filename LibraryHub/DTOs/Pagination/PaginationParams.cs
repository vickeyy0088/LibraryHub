using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Pagination
{
    public record PaginationParams
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; init; } = 1;
        [Range(1, 100)]
        public int PageSize { get; init; } = 5;
        public string? Search { get; init; }
        [Range(1, 100)]
        public int Limit { get; init; } = 10;
    }
}
