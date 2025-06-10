using System.ComponentModel.DataAnnotations;

namespace TestPlatform2.Models;

public class CategoryViewModel
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
    public string Name { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Color is required")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color code")]
    public string Color { get; set; } = "#3B82F6";
    
    [StringLength(50, ErrorMessage = "Icon class cannot exceed 50 characters")]
    public string? Icon { get; set; }
    
    public int TestCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCategoryViewModel
{
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
    public string Name { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Color is required")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color code")]
    public string Color { get; set; } = "#3B82F6";
    
    [StringLength(50, ErrorMessage = "Icon class cannot exceed 50 characters")]
    public string? Icon { get; set; }
}

public class EditCategoryViewModel
{
    [Required]
    public string Id { get; set; }
    
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
    public string Name { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Color is required")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Please enter a valid hex color code")]
    public string Color { get; set; }
    
    [StringLength(50, ErrorMessage = "Icon class cannot exceed 50 characters")]
    public string? Icon { get; set; }
}

public class CategoryManagementViewModel
{
    public IEnumerable<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
    public IEnumerable<TagViewModel> Tags { get; set; } = new List<TagViewModel>();
}