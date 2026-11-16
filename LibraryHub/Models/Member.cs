using LibraryHub.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace LibraryHub.Models;

public class Member : ISoftDelete
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string MembershipNumber { get; set; } = string.Empty;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    public DateTime MembershipDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Loan> Loans { get; set; } = [];
}
