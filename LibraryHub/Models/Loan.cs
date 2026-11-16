using LibraryHub.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LibraryHub.Models;

public class Loan : ISoftDelete // One Loan belongs to exactly One Book and One Member
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }
    // One Book can have Many Loans
    [Required]
    public Book Book { get; set; } = null!;
    [Range(1, int.MaxValue)]
    public int MemberId { get; set; }
    // One Member can have Many Loans
    [Required]
    public Member Member { get; set; } = null!;
    public DateTime LoanDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Borrowed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
