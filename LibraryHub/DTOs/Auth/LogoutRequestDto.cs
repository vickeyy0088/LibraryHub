using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Auth;

public class LogoutRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
