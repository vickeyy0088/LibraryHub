using LibraryHub.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryHub.Models;

public class Book : ISoftDelete // all books have one author & category
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(20)]
    public string ISBN { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.00", "99999999.99")]
    [Column(TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }
    [Range(0, int.MaxValue)]
    public int TotalCopies { get; set; }
    [Range(0, int.MaxValue)]
    public int AvailableCopies { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int AuthorId { get; set; }
    [Required]
    public Author Author { get; set; } = null!;
    public int CategoryId { get; set; }
    [Required]
    public Category Category { get; set; } = null!;
    // One Book can have Many Loans
    public ICollection<Loan> Loans { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    //public uint Version { get; set; }
}
