using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Loan;

public class LoanRequestDto
{
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }
    [Range(1, int.MaxValue)]
    public int MemberId { get; set; }
}
