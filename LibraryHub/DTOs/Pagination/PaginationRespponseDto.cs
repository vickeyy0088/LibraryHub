namespace LibraryHub.DTOs.Pagination;

public class PaginationResponseDto<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
}
