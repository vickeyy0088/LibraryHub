namespace LibraryHub.DTOs.Member;

public class MemberResponseDto
{
    public int Id { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public string ApplicationUserId { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime MembershipDate { get; set; }
    public bool IsActive { get; set; }
}
