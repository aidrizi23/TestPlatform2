using System.ComponentModel.DataAnnotations;

namespace TestPlatform2.Data;

public class TestCategory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(7)]
    public string Color { get; set; } = "#3B82F6"; // Default blue color
    
    [MaxLength(50)]
    public string? Icon { get; set; } // Font Awesome icon class
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign key to User
    public string UserId { get; set; }
    public User User { get; set; }
    
    // Navigation properties
    public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
}