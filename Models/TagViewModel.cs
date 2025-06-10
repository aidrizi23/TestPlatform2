using System.ComponentModel.DataAnnotations;

namespace TestPlatform2.Models;

public class TagViewModel
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "Color is required")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color code")]
    public string Color { get; set; } = "#6B7280";
    
    public int TestCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTagViewModel
{
    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "Color is required")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color code")]
    public string Color { get; set; } = "#6B7280";
}

public class EditTagViewModel
{
    [Required]
    public string Id { get; set; }
    
    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "Color is required")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color code")]
    public string Color { get; set; }
}