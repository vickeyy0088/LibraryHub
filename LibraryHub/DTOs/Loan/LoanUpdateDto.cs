namespace LibraryHub.DTOs.Loan;

public class LoanUpdateDto : LoanRequestDto
{
    public DateTime? ReturnDate { get; set; }
}
