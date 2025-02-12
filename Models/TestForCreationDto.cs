
using System.ComponentModel.DataAnnotations;

using TestPlatform2.Data;

namespace TestPlatform2.Models;

public record TestForCreationDto
{
    [Required(ErrorMessage = "Test name is required")]
    public string Title { get; init; } = "";

    public string Description { get; init; } = "";
    public bool RandomizeQuestions { get; init; } = false;

    [Required]
    [Range(1, 120, ErrorMessage = "Time limit must be between 1 and 120 minutes")]
    public int TimeLimit { get; init; } = 30;

    [Required]
    [Range(1, 10, ErrorMessage = "No more than 10 attempts allowed")]
    public int MaxAttempts { get; init; } = 1;
    
    
    // properties to set the nr of each type of question that will be shown
    [Display(Name = "Multiple Choice Questions to Show")]
    public int? MultipleChoiceQuestionsToShow { get; set; }

    [Display(Name = "True/False Questions to Show")]
    public int? TrueFalseQuestionsToShow { get; set; }

    [Display(Name = "Short Answer Questions to Show")]
    public int? ShortAnswerQuestionsToShow { get; set; }

    [Display(Name = "Questions to Show")]
    public int QuestionsToShow { get; set; }

}