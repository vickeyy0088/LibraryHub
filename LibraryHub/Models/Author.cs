using LibraryHub.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LibraryHub.Models;

public class Author : ISoftDelete  // can have multiple books
{
    public int Id { get; set; }   // we can also use Guid (Globally Unique Identifier) in place of int for Id, but we will use int for simplicity
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Biography { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Book> Books { get; set; } = new List<Book>();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
