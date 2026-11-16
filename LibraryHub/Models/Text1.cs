using System.ComponentModel.DataAnnotations;

namespace LibraryHub.Models
{
    public class Test1
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string TestName { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string TestDescription { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
