using System.ComponentModel.DataAnnotations;
using TestPlatform2.Data.Questions;

namespace TestPlatform2.Models.Questions;

public class CreateImageBasedQuestionViewModel
{
    [Required]
    public string TestId { get; set; } = "";

    [Required(ErrorMessage = "Question text is required")]
    [StringLength(2000, ErrorMessage = "Question text cannot exceed 2000 characters")]
    public string Text { get; set; } = "";

    [Range(0.1, 100, ErrorMessage = "Points must be between 0.1 and 100")]
    public double Points { get; set; } = 1;

    [Required(ErrorMessage = "Image URL is required")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string ImageUrl { get; set; } = "";

    public ImageQuestionType QuestionType { get; set; } = ImageQuestionType.Hotspot;

    public string? AltText { get; set; }

    [Range(100, 2000, ErrorMessage = "Image width must be between 100 and 2000 pixels")]
    public int ImageWidth { get; set; } = 800;

    [Range(100, 2000, ErrorMessage = "Image height must be between 100 and 2000 pixels")]
    public int ImageHeight { get; set; } = 600;

    [Required]
    public string HotspotsJson { get; set; } = "[]";

    [Required]
    public string LabelsJson { get; set; } = "[]";
}