using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Category;

public class CategoryRequestDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Description { get; set; }
}
