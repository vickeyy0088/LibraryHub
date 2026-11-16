using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    [Required]
    [RegularExpression("^(Admin|Member)$")]
    public string Role { get; set; } = "Member";
}
