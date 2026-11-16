using LibraryHub.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LibraryHub.Models;

public class Category : ISoftDelete // can have multile books of one category
{
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Book> Books { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
