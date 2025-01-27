namespace TestPlatform2.Data.Questions;

public class MultipleChoiceQuestionViewModel : QuestionViewModel
{
    public List<ChoiceOption> Options { get; set; } = new();
    public bool AllowMultipleSelections { get; set; }
    public List<string> SelectedAnswers { get; set; } = new();
}