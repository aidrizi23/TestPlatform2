namespace TestPlatform2.Models.Questions;

public class MultipleChoiceQuestionDto : QuestionDto
{
    public List<ChoiceOptionDto> Options { get; set; } = new();
    public List<string> CorrectAnswers { get; set; } = new();
    public bool AllowMultipleSelections { get; set; }
}
