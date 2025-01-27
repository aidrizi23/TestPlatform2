namespace TestPlatform2.Data.Questions;

public class CreateMultipleChoiceQuestionViewModel : CreateQuestionViewModel
{
    public List<string> Options { get; set; } = new();
    public List<string> CorrectAnswers { get; set; } = new();
    public bool AllowMultipleSelections { get; set; }
}