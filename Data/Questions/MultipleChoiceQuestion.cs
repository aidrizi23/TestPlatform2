namespace TestPlatform2.Data.Questions;

public class MultipleChoiceQuestion : Question
{
    public List<string> Options { get; set; } = new();
    public List<string> CorrectAnswers { get; set; } = new();
    public bool AllowMultipleSelections { get; set; }

    public override bool ValidateAnswer(object answer)
    {
        if (answer is not List<string> selectedAnswers) return false;
        
        return AllowMultipleSelections 
            ? selectedAnswers.All(a => CorrectAnswers.Contains(a))
            : selectedAnswers.SequenceEqual(CorrectAnswers);
    }
}