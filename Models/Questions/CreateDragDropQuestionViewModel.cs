using System.ComponentModel.DataAnnotations;

namespace TestPlatform2.Models.Questions;

public class CreateDragDropQuestionViewModel
{
    [Required]
    public string TestId { get; set; } = "";

    [Required(ErrorMessage = "Question text is required")]
    [StringLength(2000, ErrorMessage = "Question text cannot exceed 2000 characters")]
    public string Text { get; set; } = "";

    [Range(0.1, 100, ErrorMessage = "Points must be between 0.1 and 100")]
    public double Points { get; set; } = 1;

    public bool AllowMultiplePerZone { get; set; } = false;
    public bool OrderMatters { get; set; } = false;

    [Required]
    public string DraggableItemsJson { get; set; } = "[]";

    [Required]
    public string DropZonesJson { get; set; } = "[]";
}