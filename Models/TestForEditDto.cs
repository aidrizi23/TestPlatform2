using System.ComponentModel.DataAnnotations;

namespace TestPlatform2.Models;

public class TestForEditDto
{
    public string Id { get; set; }
    
    [Required(ErrorMessage = "Test name is required")]
    [StringLength(100, ErrorMessage = "Test name cannot exceed 100 characters")]
    public string TestName { get; set; }

    public string Description { get; set; }

    public bool RandomizeQuestions { get; set; }

    [Required(ErrorMessage = "Time limit is required")]
    [Range(1, 300, ErrorMessage = "Time limit must be between 1-300 minutes")]
    public int TimeLimit { get; set; }

    [Required(ErrorMessage = "Max attempts is required")]
    [Range(1, 10, ErrorMessage = "Max attempts must be between 1-10")]
    public int MaxAttempts { get; set; }
    
    
    [Display(Name = "Multiple Choice Questions to Show")]
    public int? MultipleChoiceQuestionsToShow { get; set; }

    [Display(Name = "True/False Questions to Show")]
    public int? TrueFalseQuestionsToShow { get; set; }

    [Display(Name = "Short Answer Questions to Show")]
    public int? ShortAnswerQuestionsToShow { get; set; }

    [Display(Name = "Questions to Show")]
    public int QuestionsToShow { get; set; }

}