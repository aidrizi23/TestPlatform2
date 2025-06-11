using System.ComponentModel.DataAnnotations;

namespace TestPlatform2.Data;

public class TestTag
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
    
    [Required]
    [MaxLength(7)]
    public string Color { get; set; } = "#6B7280"; // Default gray color
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign key to User
    public string UserId { get; set; }
    public User User { get; set; }
    
    // Many-to-many relationship with Tests
    public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
}