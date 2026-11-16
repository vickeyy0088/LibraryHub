namespace LibraryHub.DTOs.Report;

public class PopularBookDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int BorrowCount { get; set; }
}
