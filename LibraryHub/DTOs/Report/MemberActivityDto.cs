namespace LibraryHub.DTOs.Report;

public class MemberActivityDto
{
    public int MemberId { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public int LoanCount { get; set; }
    public int OverdueCount { get; set; }
}
