using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Member;

public class MemberCreateDto : MemberRequestDto
{
    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public DateTime? MembershipDate { get; set; }
}
