using System.ComponentModel.DataAnnotations;

namespace LibraryHub.DTOs.Book;

public class BookRequestDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalCopies { get; set; }

    [Range(1, int.MaxValue)]
    public int AuthorId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
    public uint Version { get; set; }
}
