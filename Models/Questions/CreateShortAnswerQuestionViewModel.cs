using System.ComponentModel.DataAnnotations;

namespace TestPlatform2.Data.Questions;

public class CreateShortAnswerQuestionViewModel : CreateQuestionViewModel
{
    [Required(ErrorMessage = "Expected answer is required")]
    public string ExpectedAnswer { get; set; }

    public bool CaseSensitive { get; set; }
}