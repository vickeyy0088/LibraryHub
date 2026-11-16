using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Author;

public class AuthorRequestDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? Biography { get; set; }
}
