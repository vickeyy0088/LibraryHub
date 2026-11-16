namespace LibraryHub.DTOs.Loan;

public class LoanCreateDto : LoanRequestDto
{
    public DateTime DueDate { get; set; }
}
