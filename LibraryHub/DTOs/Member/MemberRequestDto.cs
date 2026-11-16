using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Member;

public class MemberRequestDto
{
    [Required]
    [StringLength(30)]
    public string MembershipNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
